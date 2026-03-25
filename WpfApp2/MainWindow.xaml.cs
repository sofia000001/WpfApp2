using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WpfApp2;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private string currentFilePath = null;
        private bool isTextChanged = false;
        private LexicalAnalyzer lexicalAnalyzer;
        private Parser parser;
        private ObservableCollection<SyntaxError> syntaxErrors;

        public MainWindow()
        {
            InitializeComponent();
            lexicalAnalyzer = new LexicalAnalyzer();
            parser = new Parser();
            syntaxErrors = new ObservableCollection<SyntaxError>();

            ErrorsDataGrid.ItemsSource = syntaxErrors;

            UpdateStatus("Готов", 0);
        }

        // ФАЙЛ 
        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSaveChanges())
            {
                EditorTextBox.Clear();
                syntaxErrors.Clear();
                currentFilePath = null;
                isTextChanged = false;
                UpdateTitle();
                UpdateStatus("Создан новый файл", 0);
                ErrorsDataGrid.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSaveChanges())
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Текстовые файлы (*.txt)|*.txt|Java файлы (*.java)|*.java|Все файлы (*.*)|*.*";

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        EditorTextBox.Text = File.ReadAllText(dlg.FileName);
                        currentFilePath = dlg.FileName;
                        isTextChanged = false;
                        UpdateTitle();
                        UpdateStatus($"Открыт: {dlg.FileName}", 0);

                        AnalyzeText();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAs_Click(sender, e);
            }
            else
            {
                try
                {
                    File.WriteAllText(currentFilePath, EditorTextBox.Text);
                    isTextChanged = false;
                    UpdateTitle();
                    UpdateStatus($"Сохранено: {currentFilePath}", syntaxErrors.Count);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Текстовые файлы (*.txt)|*.txt|Java файлы (*.java)|*.java|Все файлы (*.*)|*.*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, EditorTextBox.Text);
                    currentFilePath = dlg.FileName;
                    isTextChanged = false;
                    UpdateTitle();
                    UpdateStatus($"Сохранено как: {dlg.FileName}", syntaxErrors.Count);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSaveChanges())
            {
                Application.Current.Shutdown();
            }
        }

        // ПРАВКА
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanUndo)
            {
                EditorTextBox.Undo();
                UpdateStatus("Отмена действия", syntaxErrors.Count);
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanRedo)
            {
                EditorTextBox.Redo();
                UpdateStatus("Возврат действия", syntaxErrors.Count);
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Cut();
            UpdateStatus("Вырезано", syntaxErrors.Count);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Copy();
            UpdateStatus("Скопировано", syntaxErrors.Count);
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Paste();
            UpdateStatus("Вставлено", syntaxErrors.Count);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectedText = "";
            UpdateStatus("Удалено", syntaxErrors.Count);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectAll();
            UpdateStatus("Выделено всё", syntaxErrors.Count);
        }

        // АНАЛИЗ
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeText();
        }

        private void ClearResults_Click(object sender, RoutedEventArgs e)
        {
            syntaxErrors.Clear();
            ErrorsDataGrid.Background = new SolidColorBrush(Colors.White);
            UpdateStatus("Результаты очищены", 0);
        }

        private void AnalyzeText()
        {
            try
            {
                syntaxErrors.Clear();

                string text = EditorTextBox.Text;

                System.Diagnostics.Debug.WriteLine($"Анализ текста: {text}");

                // Лексический анализ
                var lexicalResults = lexicalAnalyzer.Analyze(text);

                // Синтаксический анализ
                var syntaxErrorsList = parser.Parse(lexicalResults);

                // Добавляем ошибки в коллекцию (сохраняем порядок по позиции - от начала к концу)
                foreach (var error in syntaxErrorsList)
                {
                    syntaxErrors.Add(error);
                }

                int syntaxErrorsCount = syntaxErrors.Count;

                UpdateStatus("Анализ выполнен", syntaxErrorsCount);

                if (syntaxErrorsCount > 0)
                {
                    ErrorsDataGrid.Background = new SolidColorBrush(Colors.LightPink);
                }
                else
                {
                    ErrorsDataGrid.Background = new SolidColorBrush(Colors.LightGreen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // НАВИГАЦИЯ ПО ОШИБКАМ
        private void ErrorsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ErrorsDataGrid.SelectedItem is SyntaxError selectedError)
            {
                GoToErrorPosition(selectedError);
            }
        }

        private void GoToErrorPosition(SyntaxError error)
        {
            try
            {
                string[] lines = EditorTextBox.Text.Split('\n');
                int position = 0;

                for (int i = 0; i < error.Line - 1; i++)
                {
                    if (i < lines.Length)
                    {
                        position += lines[i].Length + 1;
                    }
                }

                position += error.Position - 1;

                EditorTextBox.Focus();
                EditorTextBox.Select(position, Math.Max(1, error.Fragment.Length));
                EditorTextBox.ScrollToLine(error.Line - 1);

                UpdateStatus($"Ошибка: {error.Description} (строка {error.Line}, позиция {error.Position})", syntaxErrors.Count);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка при переходе: {ex.Message}", syntaxErrors.Count);
            }
        }

        // СПРАВКА 
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText = "СИНТАКСИЧЕСКИЙ АНАЛИЗАТОР - Объявление строковых констант Java\n\n" +
                            "ОСНОВНЫЕ ФУНКЦИИ:\n" +
                            "- Введите Java код в область редактирования\n" +
                            "- Нажмите кнопку 'Пуск' или F5 для анализа\n" +
                            "- Результаты синтаксического анализа отобразятся в таблице ошибок\n\n" +
                            "СИНТАКСИЧЕСКАЯ КОНСТРУКЦИЯ:\n" +
                            "String идентификатор = выражение;\n\n" +
                            "ПРИМЕРЫ КОРРЕКТНЫХ СТРОК:\n" +
                            "String name = \"Hello\";\n" +
                            "String result = 123;\n" +
                            "String value = (10 + 20) * 2;\n\n" +
                            "ДИАГНОСТИРУЕМЫЕ ОШИБКИ:\n" +
                            "- Отсутствие ключевого слова 'String'\n" +
                            "- Отсутствие идентификатора\n" +
                            "- Отсутствие оператора '='\n" +
                            "- Отсутствие ';' в конце\n" +
                            "- Незакрытая строковая константа\n" +
                            "- Несбалансированные скобки\n" +
                            "- Некорректное выражение\n" +
                            "- Недопустимые символы (&, &&, @, #, $, % и т.д.)\n\n" +
                            "НАВИГАЦИЯ:\n" +
                            "- Двойной клик на строке с ошибкой - переход к месту ошибки";

            MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "Синтаксический анализатор строковых констант Java\n" +
                             "Лабораторная работа №3\n\n" +
                             "Автор: Дарчук Софья\n" +
                             "Группа: АП-326\n\n" +
                             "Вариант: Объявление и инициализация строковой константы\n" +
                             "на языке Java";

            MessageBox.Show(aboutText, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ 
        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isTextChanged = true;
            UpdateTitle();
            UpdateCursorPosition();
        }

        private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCursorPosition();
        }

        private void UpdateCursorPosition()
        {
            int line = EditorTextBox.GetLineIndexFromCharacterIndex(EditorTextBox.CaretIndex);
            int col = EditorTextBox.CaretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(line);

            int totalBytes = System.Text.Encoding.UTF8.GetByteCount(EditorTextBox.Text);

            CursorPositionText.Text = $"Стр.: {line + 1}  Стб.: {col + 1}  Размер: {totalBytes} байт";
        }

        private bool CheckSaveChanges()
        {
            if (isTextChanged)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Сохранить изменения в файле?",
                    "Сохранение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Save_Click(null, null);
                    return true;
                }
                else if (result == MessageBoxResult.No)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateTitle()
        {
            string title = "Синтаксический анализатор Java";
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                title += $" - {System.IO.Path.GetFileName(currentFilePath)}";
            }
            if (isTextChanged)
            {
                title += "*";
            }
            this.Title = title;
        }

        private void UpdateStatus(string message, int syntaxErrors)
        {
            StatusText.Text = message;
            if (syntaxErrors == 0)
            {
                StatsText.Text = "Анализ выполнен: ошибок нет";
            }
            else
            {
                StatsText.Text = $"Найдено ошибок: {syntaxErrors}";
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!CheckSaveChanges())
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}