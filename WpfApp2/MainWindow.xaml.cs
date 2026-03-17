using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private string currentFilePath = null;
        private bool isTextChanged = false;
        private LexicalAnalyzer lexicalAnalyzer;
        private ObservableCollection<Token> tokens;

        public MainWindow()
        {
            InitializeComponent();
            lexicalAnalyzer = new LexicalAnalyzer();
            tokens = new ObservableCollection<Token>();
            ResultsDataGrid.ItemsSource = tokens;
            UpdateStatus("Готов", 0, 0);
        }

        // ФАЙЛ 
        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSaveChanges())
            {
                EditorTextBox.Clear();
                tokens.Clear();
                currentFilePath = null;
                isTextChanged = false;
                UpdateTitle();
                UpdateStatus("Создан новый файл", 0, 0);
                ResultsDataGrid.Background = new SolidColorBrush(Colors.White);
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
                        UpdateStatus($"Открыт: {dlg.FileName}", 0, 0);

                        // Анализ при открытии
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
                    UpdateStatus($"Сохранено: {currentFilePath}",
                        CountValidTokens(), CountErrorTokens());
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
                    UpdateStatus($"Сохранено как: {dlg.FileName}",
                        CountValidTokens(), CountErrorTokens());
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
                UpdateStatus("Отмена действия", CountValidTokens(), CountErrorTokens());
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanRedo)
            {
                EditorTextBox.Redo();
                UpdateStatus("Возврат действия", CountValidTokens(), CountErrorTokens());
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Cut();
            UpdateStatus("Вырезано", CountValidTokens(), CountErrorTokens());
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Copy();
            UpdateStatus("Скопировано", CountValidTokens(), CountErrorTokens());
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Paste();
            UpdateStatus("Вставлено", CountValidTokens(), CountErrorTokens());
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectedText = "";
            UpdateStatus("Удалено", CountValidTokens(), CountErrorTokens());
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectAll();
            UpdateStatus("Выделено всё", CountValidTokens(), CountErrorTokens());
        }

        // АНАЛИЗ
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeText();
        }

        // Очистка результатов
        private void ClearResults_Click(object sender, RoutedEventArgs e)
        {
            tokens.Clear();
            ResultsDataGrid.Background = new SolidColorBrush(Colors.White);
            UpdateStatus("Результаты очищены", 0, 0);
        }

        private void AnalyzeText()
        {
            try
            {
                // Очищаем предыдущие результаты
                tokens.Clear();

                // Получаем текст из редактора
                string text = EditorTextBox.Text;

                // Для отладки
                System.Diagnostics.Debug.WriteLine($"Анализ текста: {text}");

                // Выполняем лексический анализ
                var results = lexicalAnalyzer.Analyze(text);

                // Добавляем результаты в коллекцию
                foreach (var token in results)
                {
                    tokens.Add(token);
                    // Для отладки
                    System.Diagnostics.Debug.WriteLine($"Токен: Code={token.Code}, Type={token.Type}, Value='{token.Value}', IsError={token.IsError}");
                }

                // Подсчитываем статистику
                int validCount = CountValidTokens();
                int errorCount = CountErrorTokens();

                UpdateStatus("Анализ завершен", validCount, errorCount);

                // Если есть ошибки, подсвечиваем фон DataGrid
                if (errorCount > 0)
                {
                    ResultsDataGrid.Background = new SolidColorBrush(Colors.LightPink);
                }
                else
                {
                    ResultsDataGrid.Background = new SolidColorBrush(Colors.LightGreen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Навигация по ошибкам
        private void ResultsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ResultsDataGrid.SelectedItem is Token selectedToken && selectedToken.IsError)
            {
                GoToErrorPosition(selectedToken);
            }
        }

        private void GoToErrorPosition(Token token)
        {
            try
            {
                // Разбиваем текст на строки
                string[] lines = EditorTextBox.Text.Split('\n');
                int position = 0;

                // Вычисляем глобальную позицию в тексте
                for (int i = 0; i < token.Line - 1; i++)
                {
                    if (i < lines.Length)
                    {
                        position += lines[i].Length + 1; // +1 для символа новой строки
                    }
                }

                position += token.StartPos - 1; // корректируем позицию

                // Устанавливаем курсор
                EditorTextBox.Focus();
                EditorTextBox.Select(position, Math.Max(1, token.Value.Length));
                EditorTextBox.ScrollToLine(token.Line - 1);

                UpdateStatus($"Ошибка: {token.ErrorMessage} (строка {token.Line}, позиция {token.StartPos})",
                    CountValidTokens(), CountErrorTokens());
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка при переходе: {ex.Message}",
                    CountValidTokens(), CountErrorTokens());
            }
        }

        // СПРАВКА 
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText = "СПРАВКА - Лексический анализатор строковых констант Java:\n\n" +
                            "ОСНОВНЫЕ ФУНКЦИИ:\n" +
                            "- Введите Java код в верхнюю область\n" +
                            "- Нажмите кнопку 'Пуск' или F5 для анализа\n" +
                            "- Результаты отобразятся в таблице ниже\n\n" +
                            "РАСПОЗНАВАЕМЫЕ ЛЕКСЕМЫ:\n" +
                            "- Строковые константы: \"Hello World\" (код 1)\n" +
                            "- Целые числа: 123, 42 (код 2)\n" +
                            "- Идентификаторы: переменные (код 3)\n" +
                            "- Ключевое слово String: (код 4)\n" +
                            "- Оператор присваивания =: (код 5)\n" +
                            "- Конец оператора ;: (код 6)\n" +
                            "- Пробел: (код 7)\n" +
                            "- Оператор +: (код 8)\n" +
                            "- Оператор -: (код 9)\n" +
                            "- Оператор /: (код 10)\n" +
                            "- Оператор *: (код 11)\n" +
                            "- Открывающая скобка (: (код 12)\n" +
                            "- Закрывающая скобка ): (код 13)\n\n" +
                            "ОШИБКИ (код 14):\n" +
                            "- Незакрытые строки (встречен символ \\n без закрывающей кавычки)\n" +
                            "- Недопустимые escape-последовательности\n" +
                            "- Недопустимые символы\n\n" +
                            "НАВИГАЦИЯ:\n" +
                            "- Двойной клик на строке с ошибкой - переход к месту ошибки";

            MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "Лексический анализатор строковых констант Java\n" +
                             "Лабораторная работа №2\n\n" +
                             "Автор: Дарчук Софья\n\n" +
                             "Вариант: Объявление и инициализация строковой константы\n" +
                             "на языке Java\n\n" +
                             "КОДЫ ЛЕКСЕМ:\n" +
                             "1 - строковая константа\n" +
                             "2 - целое без знака\n" +
                             "3 - идентификатор\n" +
                             "4 - ключевое слово String\n" +
                             "5 - оператор присваивания =\n" +
                             "6 - конец оператора ;\n" +
                             "7 - пробел\n" +
                             "8 - оператор +\n" +
                             "9 - оператор -\n" +
                             "10 - оператор /\n" +
                             "11 - оператор *\n" +
                             "12 - открывающая скобка (\n" +
                             "13 - закрывающая скобка )\n" +
                             "14 - ошибка";

            MessageBox.Show(aboutText, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ 
        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isTextChanged = true;
            UpdateTitle();
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
            string title = "Лексический анализатор Java";
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

        private void UpdateStatus(string message, int validTokens, int errorTokens)
        {
            StatusText.Text = message;
            StatsText.Text = $"Лексем: {validTokens + errorTokens} | Ошибок: {errorTokens}";
        }

        private int CountValidTokens()
        {
            int count = 0;
            foreach (var token in tokens)
            {
                if (!token.IsError)
                    count++;
            }
            return count;
        }

        private int CountErrorTokens()
        {
            int count = 0;
            foreach (var token in tokens)
            {
                if (token.IsError)
                    count++;
            }
            return count;
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