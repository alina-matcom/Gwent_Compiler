using System.Collections.Generic;

namespace GwentInterpreters
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;
        private bool hadError = false; // Propiedad para manejar errores

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public Expression Parse()
        {
            try
            {
                return Expression();
            }
            catch (ParseError error)
            {
                return null;
            }
        }

        private class ParseError : Exception { }

        private Expression Expression()
        {
            return Equality();
        }

        private Expression Equality()
        {
            Expression expr = Comparison();

            while (Match(TokenType.NOT_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token operador = Previous();
                Expression right = Comparison();
                expr = new BinaryExpression(expr, operador, right);
            }

            return expr;
        }

        private Expression Comparison()
        {
            Expression expr = Term();
            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token operador = Previous();
                Expression right = Term();
                expr = new BinaryExpression(expr, operador, right);
            }
            return expr;
        }

        private Expression Term()
        {
            Expression expr = Factor();
            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                Token operador = Previous();
                Expression right = Factor();
                expr = new BinaryExpression(expr, operador, right);
            }
            return expr;
        }

        private Expression Factor()
        {
            Expression expr = Unary();
            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token operador = Previous();
                Expression right = Unary();
                expr = new BinaryExpression(expr, operador, right);
            }
            return expr;
        }

        private Expression Unary()
        {
            if (Match(TokenType.NOT_EQUAL, TokenType.MINUS))
            {
                Token operador = Previous();
                Expression right = Unary();
                return new UnaryExpression(operador, right);
            }
            return Primary();
        }

        private Expression Primary()
        {
            if (Match(TokenType.BOOLEAN, TokenType.NUMBER, TokenType.STRING))
            {
                return new LiteralExpression(Previous().Literal);
            }
            if (Match(TokenType.LEFT_PAREN))
            {
                Expression expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Se esperaba ')' después de la expresión.");
                return new GroupingExpression(expr);
            }
            throw Error(Peek(), "Se espera una expresión.");
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), message);
        }

        private ParseError Error(Token token, string message)
        {
            hadError = true;
            Error(token.Location, message);
            return new ParseError();
        }

        private static void Error(CodeLocation location, string message)
        {
            Report(location.Line, location.File, message);
        }

        private static void Report(int line, string file, string message)
        {
            Console.WriteLine($"[line {line} in {file}] Error: {message}");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.SEMICOLON)
                    return;

                switch (Peek().Type)
                {
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                        return;

                }

                Advance();
            }
        }


    }
}
