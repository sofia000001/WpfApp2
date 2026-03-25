using System;
using System.Collections.Generic;
using WpfApp2;

namespace WpfApp2
{
    /// <summary>
    /// Класс ошибки синтаксического анализа
    /// </summary>
    public class SyntaxError
    {
        public string Fragment { get; set; }        // Неверный фрагмент
        public int Line { get; set; }               // Номер строки
        public int Position { get; set; }           // Позиция в строке
        public string Description { get; set; }     // Описание ошибки

        public string Location => $"строка {Line}, позиция {Position}";

        public SyntaxError(string fragment, int line, int position, string description)
        {
            Fragment = fragment;
            Line = line;
            Position = position;
            Description = description;
        }
    }

    /// <summary>
    /// Синтаксический анализатор для объявления строковых констант
    /// </summary>
    public class Parser
    {
        private List<Token> tokens;
        private int currentPos;
        private List<SyntaxError> errors;
        private Token currentToken;

        // Коды токенов из лексического анализатора
        private const int CODE_STRING = 1;
        private const int CODE_NUMBER = 2;
        private const int CODE_IDENTIFIER = 3;
        private const int CODE_KEYWORD = 4;
        private const int CODE_ASSIGN = 5;
        private const int CODE_SEMICOLON = 6;
        private const int CODE_SPACE = 7;
        private const int CODE_PLUS = 8;
        private const int CODE_MINUS = 9;
        private const int CODE_SLASH = 10;
        private const int CODE_STAR = 11;
        private const int CODE_LPAREN = 12;
        private const int CODE_RPAREN = 13;
        private const int CODE_ERROR = 14;

        public Parser()
        {
            errors = new List<SyntaxError>();
        }

        /// <summary>
        /// Основной метод синтаксического анализа
        /// </summary>
        public List<SyntaxError> Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            this.currentPos = 0;
            this.errors = new List<SyntaxError>();

            if (tokens == null || tokens.Count == 0)
            {
                AddError("", 1, 1, "Пустая строка");
                return errors;
            }

            // Пропускаем начальные пробелы
            SkipSpaces();

            if (currentToken == null)
            {
                AddError("", 1, 1, "Пустая строка");
                return errors;
            }

            try
            {
                ParseProgram();
            }
            catch (Exception ex)
            {
                AddError(currentToken?.Value ?? "конец строки",
                    currentToken?.Line ?? 1,
                    currentToken?.StartPos ?? 1,
                    $"Ошибка синтаксического анализа: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Получение следующего токена
        /// </summary>
        private Token GetNextToken()
        {
            if (currentPos < tokens.Count)
            {
                return tokens[currentPos++];
            }
            return null;
        }

        /// <summary>
        /// Переход к следующему токену
        /// </summary>
        private void Consume()
        {
            if (currentPos < tokens.Count)
            {
                currentToken = tokens[currentPos++];
            }
            else
            {
                currentToken = null;
            }
        }

        /// <summary>
        /// Пропуск пробельных токенов
        /// </summary>
        private void SkipSpaces()
        {
            while (currentPos < tokens.Count)
            {
                currentToken = tokens[currentPos];
                if (currentToken.Code == CODE_SPACE)
                {
                    currentPos++;
                }
                else
                {
                    break;
                }
            }

            if (currentPos >= tokens.Count)
            {
                currentToken = null;
            }
            else
            {
                currentToken = tokens[currentPos];
            }
        }

        /// <summary>
        /// Пропуск пробелов и переход к следующему токену
        /// </summary>
        private void NextToken()
        {
            currentPos++;
            SkipSpaces();
        }

        /// <summary>
        /// Анализ программы (Z → Объявление | ε)
        /// </summary>
        private void ParseProgram()
        {
            while (currentToken != null)
            {
                // Пропускаем пробелы
                SkipSpaces();
                if (currentToken == null) break;

                // Проверяем начало объявления
                if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
                {
                    ParseDeclaration();
                }
                else if (currentToken.Code == CODE_ERROR)
                {
                    // Лексическая ошибка уже есть, добавляем ее в синтаксические ошибки
                    AddError(currentToken.Value,
                        currentToken.Line,
                        currentToken.StartPos,
                        currentToken.ErrorMessage ?? "Лексическая ошибка: недопустимый символ");
                    NextToken();
                }
                else
                {
                    // Синтаксическая ошибка - ожидалось ключевое слово String
                    AddError(currentToken.Value,
                        currentToken.Line,
                        currentToken.StartPos,
                        "Ожидалось ключевое слово 'String'");

                    // Метод Айронса: пропускаем до следующего ключевого слова String
                    SkipToNextDeclaration();
                }
            }
        }

        /// <summary>
        /// Анализ объявления
        /// Объявление → Тип Идентификатор Оператор Присваивание ';'
        /// </summary>
        private void ParseDeclaration()
        {
            // Ожидаем ключевое слово String
            if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
            {
                NextToken(); // пропускаем String

                // Ожидаем идентификатор
                if (currentToken != null && currentToken.Code == CODE_IDENTIFIER)
                {
                    NextToken(); // пропускаем идентификатор

                    // Ожидаем оператор присваивания
                    if (currentToken != null && currentToken.Code == CODE_ASSIGN)
                    {
                        NextToken(); // пропускаем =
                        ParseAssignment(); // анализируем выражение

                        // Ожидаем точку с запятой
                        if (currentToken != null && currentToken.Code == CODE_SEMICOLON)
                        {
                            NextToken(); // пропускаем ;
                        }
                        else
                        {
                            // Проверяем, не была ли это ошибка строки
                            if (currentToken != null && currentToken.Code == CODE_STRING && !currentToken.Value.EndsWith("\""))
                            {
                                // Ошибка строки уже обработана в ParseFactor
                                // Просто пропускаем до конца
                                SkipToNextDeclaration();
                            }
                            else
                            {
                                // Ошибка: ожидалась ;
                                AddError(currentToken?.Value ?? "конец строки",
                                    currentToken?.Line ?? 1,
                                    currentToken?.StartPos ?? 1,
                                    "Ожидался символ ';' в конце оператора");

                                // Метод Айронса: пропускаем до следующего объявления
                                SkipToNextDeclaration();
                            }
                        }
                    }
                    else
                    {
                        // Ошибка: ожидался оператор =
                        AddError(currentToken?.Value ?? "конец строки",
                            currentToken?.Line ?? 1,
                            currentToken?.StartPos ?? 1,
                            "Ожидался оператор '='");

                        // Метод Айронса: пропускаем до точки с запятой
                        SkipToSemicolon();
                    }
                }
                else
                {
                    // Ошибка: ожидался идентификатор
                    AddError(currentToken?.Value ?? "конец строки",
                        currentToken?.Line ?? 1,
                        currentToken?.StartPos ?? 1,
                        "Ожидался идентификатор (имя переменной)");

                    // Метод Айронса: пропускаем до точки с запятой
                    SkipToSemicolon();
                }
            }
        }

        /// <summary>
        /// Анализ присваивания (выражения)
        /// Присваивание → Выражение
        /// </summary>
        private void ParseAssignment()
        {
            ParseExpression();
        }

        /// <summary>
        /// Анализ выражения
        /// Выражение → Терм { ('+' | '-') Терм }
        /// </summary>
        private void ParseExpression()
        {
            ParseTerm();

            while (currentToken != null &&
                   (currentToken.Code == CODE_PLUS || currentToken.Code == CODE_MINUS))
            {
                NextToken(); // пропускаем + или -
                ParseTerm();
            }
        }

        /// <summary>
        /// Анализ терма
        /// Терм → Множитель { ('*' | '/') Множитель }
        /// </summary>
        private void ParseTerm()
        {
            ParseFactor();

            while (currentToken != null &&
                   (currentToken.Code == CODE_STAR || currentToken.Code == CODE_SLASH))
            {
                NextToken(); // пропускаем * или /
                ParseFactor();
            }
        }

        /// <summary>
        /// Анализ множителя
        /// Множитель → Целое | Идентификатор | СтроковаяКонстанта | '(' Выражение ')'
        /// </summary>
        private void ParseFactor()
        {
            if (currentToken == null)
            {
                return;
            }

            // Целое число
            if (currentToken.Code == CODE_NUMBER)
            {
                NextToken();
            }
            // Идентификатор
            else if (currentToken.Code == CODE_IDENTIFIER)
            {
                NextToken();
            }
            // Строковая константа
            else if (currentToken.Code == CODE_STRING)
            {
                // Проверяем, что строка закрыта
                string value = currentToken.Value;
                if (!value.EndsWith("\""))
                {
                    // Ошибка незакрытой строки
                    AddError(value,
                        currentToken.Line,
                        currentToken.StartPos,
                        "Незакрытая строковая константа");
                }
                NextToken();
            }
            // Выражение в скобках
            else if (currentToken.Code == CODE_LPAREN)
            {
                NextToken(); // пропускаем (
                ParseExpression();
                if (currentToken != null && currentToken.Code == CODE_RPAREN)
                {
                    NextToken(); // пропускаем )
                }
                else
                {
                    AddError(currentToken?.Value ?? "конец строки",
                        currentToken?.Line ?? 1,
                        currentToken?.StartPos ?? 1,
                        "Ожидалась закрывающая скобка ')'");
                }
            }
            else if (currentToken.Code == CODE_ERROR)
            {
                // Лексическая ошибка
                AddError(currentToken.Value,
                    currentToken.Line,
                    currentToken.StartPos,
                    currentToken.ErrorMessage ?? "Недопустимый символ");
                NextToken();
            }
            else
            {
                // Ошибка: ожидалось выражение (для символов типа &, && и т.д.)
                AddError(currentToken.Value,
                    currentToken.Line,
                    currentToken.StartPos,
                    "Ожидалось выражение (число, идентификатор, строковая константа или '(')");

                // Метод Айронса: пропускаем текущий токен
                NextToken();
            }
        }

        /// <summary>
        /// Метод Айронса: пропуск токенов до точки с запятой
        /// </summary>
        private void SkipToSemicolon()
        {
            while (currentToken != null && currentToken.Code != CODE_SEMICOLON)
            {
                NextToken();
            }
            if (currentToken != null && currentToken.Code == CODE_SEMICOLON)
            {
                NextToken(); // пропускаем ;
            }
        }

        /// <summary>
        /// Метод Айронса: пропуск токенов до следующего объявления
        /// </summary>
        private void SkipToNextDeclaration()
        {
            while (currentToken != null)
            {
                if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
                {
                    break;
                }
                NextToken();
            }
        }

        /// <summary>
        /// Добавление ошибки в список
        /// </summary>
        private void AddError(string fragment, int line, int position, string description)
        {
            errors.Add(new SyntaxError(fragment, line, position, description));
        }

        /// <summary>
        /// Получение количества ошибок
        /// </summary>
        public int ErrorCount => errors.Count;
    }
}