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
        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
{
    { "effect", TokenType.EFFECT },
    { "card", TokenType.CARD },
    { "name", TokenType.NAME },
    { "type", TokenType.TYPE },
    { "faction", TokenType.FACTION },
    { "power", TokenType.POWER },
    { "range", TokenType.RANGE },
    { "action", TokenType.ACTION },
    { "params", TokenType.PARAMS },

    { "onActivation", TokenType.ONACTIVATION },
    { "selector", TokenType.SELECTOR },
    { "predicate", TokenType.PREDICATE },
    { "postAction", TokenType.POSTACTION },

    { "const", TokenType.CONST },
    { "var", TokenType.VAR },
    { "temp", TokenType.TEMP },

    { "for", TokenType.FOR },
    { "while", TokenType.WHILE },
    { "if", TokenType.IF },
    { "else", TokenType.ELSE },

    { "triggerPlayer", TokenType.TRIGGERPLAYER },
    { "board", TokenType.BOARD },
    { "handOfPlayer", TokenType.HANDOFPLAYER },
    { "fieldOfPlayer", TokenType.FIELD_OF_PLAYER },
    { "graveyardOfPlayer", TokenType.GRAVEYARD_OF_PLAYER },
    { "deckOfPlayer", TokenType.DECK_OF_PLAYER },
    { "owner", TokenType.OWNER },
    { "find", TokenType.FIND },
    { "push", TokenType.PUSH },
    { "sendBottom", TokenType.SENDBOTTOM },
    { "pop", TokenType.POP },
    { "remove", TokenType.REMOVE },
    { "shuffle", TokenType.SHUFFLE },

    // Operadores lógicos
    { "and", TokenType.AND },
    { "or", TokenType.OR },
    { "not", TokenType.NOT },
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
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case '-':
                    AddToken(TokenType.MINUS);
                    break;
                case '+':
                    AddToken(TokenType.PLUS);
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
                case '!':
                    AddToken(Match('=') ? TokenType.NOT_EQUAL : TokenType.NOT);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.ASSIGN);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
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
                        throw new Error(ErrorType.LEXICAL, $"Character '{c}' is not supported.");
                    }
                    break;
            }
        }

        private void Identifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();
            string text = source.Substring(start, current - start);
            TokenType type = keywords.ContainsKey(text) ? keywords[text] : TokenType.IDENTIFIER;
            AddToken(type);

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
            while (IsDigit(Peek())) Advance();

            // Buscamos la parte fraccional.
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consumimos el "."
                Advance();
                while (IsDigit(Peek())) Advance();
            }

            // Convertimos el lexema a double.
            string numberStr = source.Substring(start, current - start);
            double number = double.Parse(numberStr);
            AddToken(TokenType.NUMBER, number);
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
            // Aquí puedes implementar la lógica para manejar errores.
            // Por ejemplo, podrías imprimir el error o añadirlo a una lista de errores.
            Console.WriteLine($"[Line {line}] Error: {message}");
        }
    }
}
