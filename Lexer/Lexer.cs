using System;
using System.Collections.Generic;

namespace GwentInterpreters
{
    public class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;
        public static bool HadError { get; private set; } = false; // Variable para manejar errores
        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "effect", TokenType.EFFECT_DECLARATION },
            { "Effect", TokenType.EFFECT_CALL },
            { "card", TokenType.CARD },
            { "OnActivation", TokenType.ONACTIVATION },
            { "Params", TokenType.PARAMS },
            { "Action", TokenType.ACTION },
            { "Selector", TokenType.SELECTOR },
            { "Source", TokenType.SOURCE },
            { "Single", TokenType.SINGLE },
            { "Predicate", TokenType.PREDICATE },
            { "PostAction", TokenType.POSTACTION },
            { "Number", TokenType.NUMBER_SPECIFIER },
            { "Boolean", TokenType.BOOLEAN_SPECIFIER },
            { "String", TokenType.STRING_SPECIFIER },
            { "for", TokenType.FOR },
            { "in", TokenType.IN },
            { "while", TokenType.WHILE },
            { "true", TokenType.BOOLEAN },
            { "false", TokenType.BOOLEAN },
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "and", TokenType.AND },
            { "or", TokenType.OR },
            { "not", TokenType.NOT }
        };

        public Scanner(string source)
        {
            this.source = source;
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // Estamos al inicio del siguiente lexema
                start = current;
                ScanToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, new CodeLocation { File = "", Line = line, Column = current }));
            return tokens;
        }

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(':
                    AddToken(TokenType.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenType.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenType.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenType.RIGHT_BRACE);
                    break;
                case '[':  // Manejar el carácter '['
                    AddToken(TokenType.LEFT_BRACKET);
                    break;
                case ']':  // Manejar el carácter ']'
                    AddToken(TokenType.RIGHT_BRACKET);
                    break;
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case '-':
                    if (Match('='))
                    {
                        AddToken(TokenType.MINUS_EQUAL);
                    }
                    else if (Match('-'))
                    {
                        AddToken(TokenType.DECREMENT);
                    }
                    else
                    {
                        AddToken(TokenType.MINUS);
                    }
                    break;
                case '+':
                    if (Match('='))
                    {
                        AddToken(TokenType.PLUS_EQUAL);
                    }
                    else if (Match('+'))
                    {
                        AddToken(TokenType.INCREMENT);
                    }
                    else
                    {
                        AddToken(TokenType.PLUS);
                    }
                    break;
                case ';':
                    AddToken(TokenType.SEMICOLON);
                    break;
                case '*':
                    AddToken(TokenType.STAR);
                    break;
                case '/':
                    AddToken(TokenType.SLASH);
                    break;
                case ':':
                    AddToken(TokenType.COLON);
                    break;
                case '!':
                    AddToken(Match('=') ? TokenType.NOT_EQUAL : TokenType.NOT);
                    break;
                case '=':
                    if (Match('='))
                    {
                        AddToken(TokenType.EQUAL_EQUAL);
                    }
                    else if (Match('>'))
                    {
                        AddToken(TokenType.LAMBDA);
                    }
                    else
                    {
                        AddToken(TokenType.ASSIGN);
                    }
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;

                case '@':
                    if (Match('@'))
                    {
                        AddToken(TokenType.DOUBLE_CONCAT);
                    }
                    else
                    {
                        AddToken(TokenType.CONCAT);
                    }
                    break;
                case ' ':
                case '\r':
                case '\t':
                    // Ignorar espacios en blanco.
                    break;
                case '\n':
                    line++;
                    break;
                case '"':
                    String();
                    break;
                default:
                    if (IsDigit(c))
                    {
                        Number();
                    }
                    else if (IsAlpha(c))
                    {
                        Identifier();
                    }
                    else
                    {
                        Error(line, $"Carácter inesperado: {c}");
                    }
                    break;
            }
        }

        private void Identifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();
            string text = source.Substring(start, current - start);
            TokenType type = keywords.ContainsKey(text) ? keywords[text] : TokenType.IDENTIFIER;
            object literal = null;

            // Asigna el valor booleano si la palabra clave es "false"
            if (text == "false")
            {
                literal = false;
            }
            // Puedes agregar lógica similar para otras palabras clave booleanas como "true"
            else if (text == "true")
            {
                literal = true;
            }

            AddToken(type, literal);
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        private bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }
        private void Number()
        {
            bool hasDecimal = false;

            while (IsDigit(Peek()) || (Peek() == '.' && !hasDecimal && IsDigit(PeekNext())))
            {
                if (Peek() == '.')
                {
                    hasDecimal = true;
                }
                Advance();
            }

            // Si encontramos un segundo punto decimal o terminamos en un punto sin un número después
            if (Peek() == '.' || (hasDecimal && !IsDigit(PeekNext())))
            {
                // Consumir el resto de la secuencia hasta que se encuentre un delimitador
                while (IsDigit(Peek()) || Peek() == '.')
                {
                    Advance();
                }

                string invalidNumber = source.Substring(start, current - start);
                Error(line, $"Número mal formado: {invalidNumber}");
            }
            else
            {
                // Convertimos el lexema a double.
                string numberStr = source.Substring(start, current - start);
                double number = double.Parse(numberStr);
                AddToken(TokenType.NUMBER, number);
            }
        }
        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        private char PeekNext()
        {
            if (current + 1 >= source.Length) return '\0';
            return source[current + 1];
        }

        private void String()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') line++;
                Advance();
            }

            if (IsAtEnd())
            {
                Error(line, "Cadena no terminada.");
                return;
            }

            // El cierre de la comilla ".
            Advance();

            // Recortamos las comillas que rodean la cadena.
            string value = source.Substring(start + 1, current - start - 2);
            AddToken(TokenType.STRING, value);
        }

        private char Advance()
        {
            current++;
            return source[current - 1];
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object literal)
        {
            string text = source.Substring(start, current - start);
            tokens.Add(new Token(type, text, literal, new CodeLocation { File = "", Line = line, Column = current }));
        }

        private bool IsAtEnd()
        {
            return current >= source.Length;
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (source[current] != expected) return false;
            current++;
            return true;
        }

        private void Error(int line, string message)
        {
            Report(line, "", message);
            HadError = true; // Marcamos que hubo un error
        }

        private void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        }
    }
}
