using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp2
{
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

        private readonly HashSet<string> keywords = new HashSet<string>
        {
            "String", "int", "double", "boolean", "char", "byte", "short", "long", "float"
        };

        // Проверка, является ли строка корректным идентификатором 
        private bool IsValidIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s) || !(char.IsLetter(s[0]) || s[0] == '_'))
                return false;
            for (int i = 1; i < s.Length; i++)
            {
                if (!char.IsLetterOrDigit(s[i]) && s[i] != '_')
                    return false;
            }
            return true;
        }

        // Проверка, является ли строка корректным числом 
        private bool IsValidNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        // Проверка, является ли последовательность полностью корректной лексемой
        private bool IsFullyValidToken(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }

        // Проверка, является ли символ допустимым для корректной лексемы
        private bool IsValidCharForToken(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();
            int lineNumber = 1;
            int position = 1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Перевод строки
                if (c == '\n')
                {
                    lineNumber++;
                    position = 1;
                    continue;
                }

                // Возврат 
                if (c == '\r')
                {
                    position++;
                    continue;
                }

                // Пробелы
                if (c == ' ' || c == '\t')
                {
                    tokens.Add(CreateToken(CODE_SPACE, "пробел", " ", lineNumber, position));
                    position++;
                    continue;
                }

                // Комментарии
                if (c == '/' && i + 1 < text.Length)
                {
                    char nextChar = text[i + 1];
                    if (nextChar == '/')
                    {
                        while (i < text.Length && text[i] != '\n') i++;
                        if (i < text.Length && text[i] == '\n')
                        {
                            lineNumber++;
                            position = 1;
                        }
                        continue;
                    }
                    else if (nextChar == '*')
                    {
                        i += 2;
                        while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                        {
                            if (text[i] == '\n')
                            {
                                lineNumber++;
                                position = 1;
                            }
                            i++;
                        }
                        i += 2;
                        continue;
                    }
                }

                // Строковые константы

                if (c == '"')
                {
                    int startPos = position;
                    int startLine = lineNumber;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(c);
                    i++;
                    position++;

                    while (i < text.Length && text[i] != '"' && text[i] != '\n')
                    {
                        if (text[i] == '\\')
                        {
                            sb.Append(text[i]);
                            i++;
                            position++;
                            if (i < text.Length)
                            {
                                sb.Append(text[i]);
                                i++;
                                position++;
                            }
                        }
                        else
                        {
                            sb.Append(text[i]);
                            i++;
                            position++;
                        }
                    }

                    if (i < text.Length && text[i] == '"')
                    {
                        sb.Append(text[i]);
                        i++;
                        position++;
                        tokens.Add(new Token
                        {
                            Code = CODE_STRING,
                            Type = "строковая константа",
                            Value = sb.ToString(),
                            Line = startLine,
                            StartPos = startPos,
                            EndPos = position - 1
                        });
                    }
                    else
                    {
                        tokens.Add(new Token
                        {
                            Code = CODE_ERROR,
                            Type = "ОШИБКА: Незакрытая строковая константа",
                            Value = sb.ToString(),
                            Line = startLine,
                            StartPos = startPos,
                            EndPos = position - 1,
                            IsError = true,
                            ErrorLine = startLine,
                            ErrorMessage = "Незакрытая строковая константа"
                        });
                    }

                    i--;
                    continue;
                }

                // односимвольные операторы

                if (c == '=' || c == ';' || c == '+' || c == '-' || c == '/' || c == '*' || c == '(' || c == ')')
                {
                    int code = c == '=' ? CODE_ASSIGN :
                              c == ';' ? CODE_SEMICOLON :
                              c == '+' ? CODE_PLUS :
                              c == '-' ? CODE_MINUS :
                              c == '/' ? CODE_SLASH :
                              c == '*' ? CODE_STAR :
                              c == '(' ? CODE_LPAREN : CODE_RPAREN;

                    string type = c == '=' ? "оператор присваивания" :
                                 c == ';' ? "конец оператора" :
                                 c == '+' ? "оператор +" :
                                 c == '-' ? "оператор -" :
                                 c == '/' ? "оператор /" :
                                 c == '*' ? "оператор *" :
                                 c == '(' ? "открывающая скобка" : "закрывающая скобка";

                    tokens.Add(CreateToken(code, type, c.ToString(), lineNumber, position));
                    position++;
                    continue;
                }

                // корректные идентификаторы и числа

                if (char.IsLetter(c) || char.IsDigit(c) || c == '_')
                {
                    int startPos = position;
                    int startLine = lineNumber;
                    StringBuilder sb = new StringBuilder();

                    while (i < text.Length)
                    {
                        char currentChar = text[i];
                        if (!char.IsLetterOrDigit(currentChar) && currentChar != '_')
                            break;
                        sb.Append(currentChar);
                        i++;
                        position++;
                    }

                    string tokenValue = sb.ToString();

                    // Проверяем, что после последовательности идет допустимый разделитель
                    bool hasValidSeparator = true;
                    if (i < text.Length)
                    {
                        char nextChar = text[i];
                        if (!(nextChar == ' ' || nextChar == '\t' || nextChar == '\n' || nextChar == '\r' ||
                              nextChar == '=' || nextChar == ';' || nextChar == '+' || nextChar == '-' ||
                              nextChar == '/' || nextChar == '*' || nextChar == '(' || nextChar == ')' ||
                              nextChar == '"'))
                        {
                            hasValidSeparator = false;
                        }
                    }

                    if (hasValidSeparator)
                    {
                        if (keywords.Contains(tokenValue))
                        {
                            tokens.Add(CreateToken(CODE_KEYWORD, "ключевое слово", tokenValue, startLine, startPos));
                        }
                        else if (IsValidNumber(tokenValue))
                        {
                            tokens.Add(CreateToken(CODE_NUMBER, "целое без знака", tokenValue, startLine, startPos));
                        }
                        else if (IsValidIdentifier(tokenValue))
                        {
                            tokens.Add(CreateToken(CODE_IDENTIFIER, "идентификатор", tokenValue, startLine, startPos));
                        }
                        else
                        {
                            tokens.Add(CreateToken(CODE_ERROR, "ОШИБКА: Некорректная лексема", tokenValue, startLine, startPos));
                        }
                        i--;
                        continue;
                    }
                    else
                    {
                        // Если после последовательности идет недопустимый символ - вся последовательность становится ошибкой
                        sb.Append(text[i]);
                        i++;
                        position++;

                        while (i < text.Length)
                        {
                            char currentChar = text[i];
                            if (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r' ||
                                currentChar == '=' || currentChar == ';' || currentChar == '"')
                            {
                                break;
                            }
                            sb.Append(currentChar);
                            i++;
                            position++;
                        }

                        tokens.Add(new Token
                        {
                            Code = CODE_ERROR,
                            Type = "ОШИБКА: Недопустимые символы",
                            Value = sb.ToString(),
                            Line = startLine,
                            StartPos = startPos,
                            EndPos = position - 1,
                            IsError = true,
                            ErrorLine = startLine,
                            ErrorMessage = $"Недопустимые символы: {sb}"
                        });
                        i--;
                        continue;
                    }
                }

                // любые другие символы

                int errorStartPos = position;
                int errorStartLine = lineNumber;
                StringBuilder errorGroup = new StringBuilder();

                while (i < text.Length)
                {
                    char currentChar = text[i];
                    if (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r' ||
                        currentChar == '=' || currentChar == ';' || currentChar == '"')
                    {
                        break;
                    }
                    errorGroup.Append(currentChar);
                    i++;
                    position++;
                }

                if (errorGroup.Length > 0)
                {
                    tokens.Add(new Token
                    {
                        Code = CODE_ERROR,
                        Type = "ОШИБКА: Недопустимые символы",
                        Value = errorGroup.ToString(),
                        Line = errorStartLine,
                        StartPos = errorStartPos,
                        EndPos = position - 1,
                        IsError = true,
                        ErrorLine = errorStartLine,
                        ErrorMessage = $"Недопустимые символы: {errorGroup}"
                    });
                }

                i--;
            }

            return tokens;
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
                EndPos = pos + value.Length - 1
            };
        }
    }
}