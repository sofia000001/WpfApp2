using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1
{
    // Класс Token
    public class Token
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public bool IsError { get; set; }
        public int ErrorLine { get; set; }
        public string ErrorMessage { get; set; }

        public string Location
        {
            get
            {
                if (IsError)
                {
                    return $"строка {ErrorLine}, позиция {StartPos}";
                }
                else
                {
                    return $"строка {Line}, {StartPos}-{EndPos}";
                }
            }
        }
    }

    public class LexicalAnalyzer
    {
        // ЛЕКСЕМЫ
        private const int CODE_STRING = 1;           // строковая константа
        private const int CODE_NUMBER = 2;            // целое без знака
        private const int CODE_IDENTIFIER = 3;        // идентификатор
        private const int CODE_KEYWORD = 4;           // ключевое слово
        private const int CODE_ASSIGN = 5;            // оператор присваивания =
        private const int CODE_SEMICOLON = 6;         // конец оператора ;
        private const int CODE_SPACE = 7;              // пробел
        private const int CODE_PLUS = 8;               // оператор +
        private const int CODE_MINUS = 9;              // оператор -
        private const int CODE_SLASH = 10;             // оператор /
        private const int CODE_STAR = 11;              // оператор *
        private const int CODE_LPAREN = 12;            // открывающая скобка (
        private const int CODE_RPAREN = 13;            // закрывающая скобка )
        private const int CODE_ERROR = 14;              // ошибка

        // Множество ключевых слов Java
        private readonly HashSet<string> keywords = new HashSet<string>
        {
            "String", "int", "double", "boolean", "char", "byte", "short", "long", "float",
            "abstract", "assert", "break", "case", "catch", "class", "const", "continue",
            "default", "do", "else", "enum", "extends", "final", "finally", "for",
            "goto", "if", "implements", "import", "instanceof", "interface",
            "native", "new", "package", "private", "protected", "public",
            "return", "static", "strictfp", "super", "switch", "synchronized",
            "this", "throw", "throws", "transient", "try", "void", "volatile", "while",
            "true", "false", "null"
        };

        // Состояния конечного автомата
        private enum State
        {
            START,
            IN_STRING,
            IN_NUMBER,
            IN_IDENT,
            IN_ESCAPE,
        }

        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();
            int lineNumber = 1;
            int position = 1;
            int tokenStartLine = 1;
            int tokenStartPos = 1;

            State currentState = State.START;
            StringBuilder currentToken = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char nextChar = i < text.Length - 1 ? text[i + 1] : '\0';

                // Обработка перевода строки
                if (c == '\n')
                {
                    // Завершаем текущий токен если есть
                    if (currentState != State.START)
                    {
                        if (currentState == State.IN_STRING)
                        {
                            tokens.Add(new Token
                            {
                                Code = CODE_ERROR,
                                Type = "ОШИБКА: Незакрытая строковая константа",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position - 1,
                                IsError = true,
                                ErrorLine = lineNumber,
                                ErrorMessage = "Незакрытая строковая константа"
                            });
                        }
                        else if (currentState == State.IN_ESCAPE)
                        {
                            tokens.Add(new Token
                            {
                                Code = CODE_ERROR,
                                Type = "ОШИБКА: Незавершенная escape-последовательность",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position - 1,
                                IsError = true,
                                ErrorLine = lineNumber,
                                ErrorMessage = "Незавершенная escape-последовательность"
                            });
                        }
                        else
                        {
                            CompleteToken(tokens, currentState, currentToken.ToString(), tokenStartLine, tokenStartPos, position - 1);
                        }
                        currentState = State.START;
                        currentToken.Clear();
                    }

                    lineNumber++;
                    position = 1;
                    continue;
                }

                // Обработка текущего состояния
                switch (currentState)
                {
                    case State.START:
                        tokenStartLine = lineNumber;
                        tokenStartPos = position;

                        // Проверяем все допустимые символы
                        if (c == '"')
                        {
                            currentState = State.IN_STRING;
                            currentToken.Append(c);
                        }
                        else if (char.IsDigit(c))
                        {
                            currentState = State.IN_NUMBER;
                            currentToken.Append(c);
                        }
                        else if (char.IsLetter(c))
                        {
                            currentState = State.IN_IDENT;
                            currentToken.Append(c);
                        }
                        else if (c == '=')
                        {
                            tokens.Add(CreateToken(CODE_ASSIGN, "оператор присваивания", "=", lineNumber, position));
                        }
                        else if (c == ';')
                        {
                            tokens.Add(CreateToken(CODE_SEMICOLON, "конец оператора", ";", lineNumber, position));
                        }
                        else if (c == ' ' || c == '\t')
                        {
                            tokens.Add(CreateToken(CODE_SPACE, "пробел", " ", lineNumber, position));
                        }
                        else if (c == '+')
                        {
                            tokens.Add(CreateToken(CODE_PLUS, "оператор +", "+", lineNumber, position));
                        }
                        else if (c == '-')
                        {
                            tokens.Add(CreateToken(CODE_MINUS, "оператор -", "-", lineNumber, position));
                        }
                        else if (c == '/')
                        {
                            // Проверка на комментарий
                            if (nextChar == '/')
                            {
                                // Пропускаем однострочный комментарий
                                while (i < text.Length && text[i] != '\n')
                                {
                                    i++;
                                }
                                if (i < text.Length && text[i] == '\n')
                                {
                                    lineNumber++;
                                    position = 1;
                                }
                            }
                            else if (nextChar == '*')
                            {
                                // Пропускаем многострочный комментарий
                                i += 2;
                                position += 2;
                                while (i < text.Length - 1)
                                {
                                    if (text[i] == '*' && text[i + 1] == '/')
                                    {
                                        i += 2;
                                        position += 2;
                                        break;
                                    }
                                    if (text[i] == '\n')
                                    {
                                        lineNumber++;
                                        position = 1;
                                    }
                                    else
                                    {
                                        position++;
                                    }
                                    i++;
                                }
                            }
                            else
                            {
                                tokens.Add(CreateToken(CODE_SLASH, "оператор /", "/", lineNumber, position));
                            }
                        }
                        else if (c == '*')
                        {
                            tokens.Add(CreateToken(CODE_STAR, "оператор *", "*", lineNumber, position));
                        }
                        else if (c == '(')
                        {
                            tokens.Add(CreateToken(CODE_LPAREN, "открывающая скобка", "(", lineNumber, position));
                        }
                        else if (c == ')')
                        {
                            tokens.Add(CreateToken(CODE_RPAREN, "закрывающая скобка", ")", lineNumber, position));
                        }
                        else if (c == '\r')
                        {

                        }
                        else
                        {
                            // Недопустимый символ
                            string displayValue;
                            if (c < 32) // Управляющие символы
                            {
                                displayValue = $"\\u{(int)c:X4}";
                            }
                            else
                            {
                                displayValue = c.ToString();
                            }

                            tokens.Add(new Token
                            {
                                Code = CODE_ERROR,
                                Type = $"ОШИБКА: Недопустимый символ",
                                Value = displayValue,
                                Line = lineNumber,
                                StartPos = position,
                                EndPos = position,
                                IsError = true,
                                ErrorLine = lineNumber,
                                ErrorMessage = $"Недопустимый символ (код {(int)c})"
                            });
                        }
                        break;

                    case State.IN_STRING:
                        if (c == '"' && (currentToken.Length == 0 || currentToken[currentToken.Length - 1] != '\\'))
                        {
                            currentToken.Append(c);
                            tokens.Add(new Token
                            {
                                Code = CODE_STRING,
                                Type = "строковая константа",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position
                            });
                            currentState = State.START;
                            currentToken.Clear();
                        }
                        else if (c == '\\')
                        {
                            currentState = State.IN_ESCAPE;
                            currentToken.Append(c);
                        }
                        else
                        {
                            currentToken.Append(c);
                        }
                        break;

                    case State.IN_ESCAPE:
                        if (IsValidEscapeSequence(c))
                        {
                            currentToken.Append(c);
                            currentState = State.IN_STRING;
                        }
                        else
                        {
                            currentToken.Append(c);
                            tokens.Add(new Token
                            {
                                Code = CODE_ERROR,
                                Type = $"ОШИБКА: Недопустимая escape-последовательность",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position,
                                IsError = true,
                                ErrorLine = lineNumber,
                                ErrorMessage = $"Недопустимая escape-последовательность '\\{c}'"
                            });
                            currentState = State.START;
                            currentToken.Clear();
                        }
                        break;

                    case State.IN_NUMBER:
                        if (char.IsDigit(c))
                        {
                            currentToken.Append(c);
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = CODE_NUMBER,
                                Type = "целое без знака",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position - 1
                            });
                            currentState = State.START;
                            currentToken.Clear();
                            i--;
                            position--;
                        }
                        break;

                    case State.IN_IDENT:
                        if (char.IsLetterOrDigit(c) || c == '_')
                        {
                            currentToken.Append(c);
                        }
                        else
                        {
                            string ident = currentToken.ToString();
                            tokens.Add(new Token
                            {
                                Code = keywords.Contains(ident) ? CODE_KEYWORD : CODE_IDENTIFIER,
                                Type = keywords.Contains(ident) ? "ключевое слово" : "идентификатор",
                                Value = ident,
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = position - 1
                            });
                            currentState = State.START;
                            currentToken.Clear();
                            i--;
                            position--;
                        }
                        break;
                }

                position++;
            }

            // Обработка оставшихся токенов в конце файла
            if (currentState != State.START)
            {
                if (currentState == State.IN_STRING)
                {
                    tokens.Add(new Token
                    {
                        Code = CODE_ERROR,
                        Type = "ОШИБКА: Незакрытая строковая константа",
                        Value = currentToken.ToString(),
                        Line = tokenStartLine,
                        StartPos = tokenStartPos,
                        EndPos = text.Length,
                        IsError = true,
                        ErrorLine = lineNumber,
                        ErrorMessage = "Незакрытая строковая константа"
                    });
                }
                else if (currentState == State.IN_ESCAPE)
                {
                    tokens.Add(new Token
                    {
                        Code = CODE_ERROR,
                        Type = "ОШИБКА: Незавершенная escape-последовательность",
                        Value = currentToken.ToString(),
                        Line = tokenStartLine,
                        StartPos = tokenStartPos,
                        EndPos = text.Length,
                        IsError = true,
                        ErrorLine = lineNumber,
                        ErrorMessage = "Незавершенная escape-последовательность"
                    });
                }
                else
                {
                    CompleteToken(tokens, currentState, currentToken.ToString(), tokenStartLine, tokenStartPos, text.Length);
                }
            }

            return tokens;
        }

        private void CompleteToken(List<Token> tokens, State state, string value, int line, int startPos, int endPos)
        {
            if (string.IsNullOrEmpty(value)) return;

            switch (state)
            {
                case State.IN_NUMBER:
                    tokens.Add(new Token
                    {
                        Code = CODE_NUMBER,
                        Type = "целое без знака",
                        Value = value,
                        Line = line,
                        StartPos = startPos,
                        EndPos = endPos
                    });
                    break;
                case State.IN_IDENT:
                    tokens.Add(new Token
                    {
                        Code = keywords.Contains(value) ? CODE_KEYWORD : CODE_IDENTIFIER,
                        Type = keywords.Contains(value) ? "ключевое слово" : "идентификатор",
                        Value = value,
                        Line = line,
                        StartPos = startPos,
                        EndPos = endPos
                    });
                    break;
            }
        }

        private bool IsValidEscapeSequence(char c)
        {
            string validEscapes = "\"\\'nrtbfu";
            return validEscapes.Contains(c.ToString());
        }

        private Token CreateToken(int code, string type, string value, int line, int pos)
        {
            return new Token
            {
                Code = code,
                Type = type,
                Value = value,
                Line = line,
                StartPos = pos,
                EndPos = pos
            };
        }
    }
}