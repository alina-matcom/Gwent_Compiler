using System.Collections.Generic;
using System.Linq.Expressions;

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


        public List<Stmt> Parse()
        {
            List<Stmt> statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }
            return statements;
        }

        private Stmt Declaration()
        {
            try
            {
                if (Match(TokenType.EFFECT)) return EffectDeclaration();
                if (Match(TokenType.CARD)) return CardDeclaration();


                throw Error(Peek(), "Expected 'effect' or 'card' declaration.");
            }
            catch (ParseError error)
            {
                Synchronize();
                return null;
            }
        }
        private Stmt EffectDeclaration()
        {
            // Consumimos la apertura del bloque '{'
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'effect'.");

            // Parseo del nombre del efecto
            Consume(TokenType.NAME, "Se esperaba la clave 'Name' dentro del efecto.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Name'.");
            string name = Consume(TokenType.STRING, "Se esperaba un nombre de efecto.").Lexeme;

            // Consumimos la coma después del nombre
            Consume(TokenType.COMMA, "Se esperaba ',' después del nombre del efecto.");

            // Inicialización de la lista de parámetros (opcional)
            List<Parameter> parameters = null;

            // Verificamos si 'Params' está presente
            if (Match(TokenType.PARAMS))
            {
                Consume(TokenType.COLON, "Se esperaba ':' después de 'Params'.");
                parameters = ParseParams();

                // Consumimos la coma después de los parámetros
                Consume(TokenType.COMMA, "Se esperaba ',' después del bloque de parámetros.");
            }

            // Parseo de la acción
            Consume(TokenType.ACTION, "Se esperaba la clave 'Action' dentro del efecto.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Action'.");
            Action action = ParseAction();

            // Consumimos la clausura del bloque '}'
            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración de efecto.");

            // Retornamos un nodo de declaración de efecto con todos los campos necesarios
            return new EffectStmt(name, parameters, action);
        }

        private List<Parameter> ParseParams()
        {
            var parameters = new List<Parameter>();

            // Consumimos '{' que abre el bloque de parámetros
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'Params'.");

            // Parseamos todos los pares clave-tipo dentro del bloque de parámetros
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                // Consumimos el nombre del parámetro
                Token paramName = Consume(TokenType.IDENTIFIER, "Se esperaba un nombre de parámetro.");

                // Consumimos ':'
                Consume(TokenType.COLON, "Se esperaba ':' después del nombre del parámetro.");

                // Parseamos el tipo del parámetro usando los especificadores
                Token paramType = ParseTypeToken();

                // Creamos una nueva instancia de Property y la añadimos a la lista
                parameters.Add(new Parameter(paramName, paramType));

                // Si hay una coma, avanzamos para el siguiente parámetro
                if (!Match(TokenType.COMMA))
                {
                    break;
                }
            }

            // Consumimos '}' que cierra el bloque de parámetros
            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después de los parámetros.");

            return parameters;
        }

        private Token ParseTypeToken()
        {
            if (Match(TokenType.NUMBER_SPECIFIER)) return Previous();
            if (Match(TokenType.STRING_SPECIFIER)) return Previous();
            if (Match(TokenType.BOOLEAN_SPECIFIER)) return Previous();
            throw Error(Peek(), "Tipo no válido.");
        }
        private Action ParseAction()
        {
            Consume(TokenType.LEFT_PAREN, "Se esperaba '(' después de 'Action'.");
            Token targetParam = Consume(TokenType.IDENTIFIER, "Se esperaba el parámetro objetivo.");
            Consume(TokenType.COMMA, "Se esperaba ',' después del parámetro objetivo.");
            Token contextParam = Consume(TokenType.IDENTIFIER, "Se esperaba el parámetro de contexto.");
            Consume(TokenType.RIGHT_PAREN, "Se esperaba ')' después de los parámetros de la acción.");
            Consume(TokenType.LAMBDA, "Se esperaba '=>' después de los parámetros de la acción.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' antes del cuerpo de la acción.");
            var body = Block();
            // No es necesario consumir '}' aquí, ya que Block() lo maneja

            return new Action(targetParam, contextParam, body);
        }


        private Stmt CardDeclaration()
        {
            Consume(TokenType.CARD, "Se esperaba la palabra clave 'card'.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'card'.");

            // Parseo de los atributos de la carta
            string type = ParseStringAttribute(TokenType.TYPE, "Type");
            string name = ParseStringAttribute(TokenType.NAME, "Name");
            string faction = ParseStringAttribute(TokenType.FACTION, "Faction");
            Expression power = ParseExpressionAttribute(TokenType.POWER, "Power");
            List<string> range = ParseStringListAttribute(TokenType.RANGE, "Range");

            // Parseo de la lista de efectos en OnActivation
            Consume(TokenType.ONACTIVATION, "Se esperaba la clave 'OnActivation'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'OnActivation'.");
            List<Effect> onActivation = ParseEffects();

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración de carta.");

            return new CardStmt(type, name, faction, power, range, onActivation);
        }

        private string ParseStringAttribute(TokenType expectedTokenType, string attributeName)
        {
            Consume(expectedTokenType, $"Se esperaba la clave '{attributeName}'.");
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");
            return Consume(TokenType.STRING, $"Se esperaba un valor de cadena para '{attributeName}'.").Lexeme;
        }

        private List<string> ParseStringListAttribute(TokenType expectedTokenType, string attributeName)
        {
            Consume(expectedTokenType, $"Se esperaba la clave '{attributeName}'.");
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");
            Consume(TokenType.LEFT_BRACKET, $"Se esperaba '[' después de ':' en '{attributeName}'.");

            List<string> values = new List<string>();
            do
            {
                values.Add(Consume(TokenType.STRING, $"Se esperaba un valor de cadena en la lista de '{attributeName}'.").Lexeme);
            } while (Match(TokenType.COMMA));

            Consume(TokenType.RIGHT_BRACKET, $"Se esperaba ']' al final de la lista de '{attributeName}'.");

            return values;
        }

        private Expression ParseExpressionAttribute(TokenType expectedTokenType, string attributeName)
        {
            Consume(expectedTokenType, $"Se esperaba la clave '{attributeName}'.");
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");

            // Parse the expression and ensure it's numeric
            Expression expr = Expression();

            // Add a check here to validate that the expression is numeric
            if (!IsNumericExpression(expr))
            {
                throw Error(Peek(), $"La expresión para '{attributeName}' debe ser numérica.");
            }

            return expr;
        }

        private bool IsNumericExpression(Expression expr)
        {
            // Recursively check if the expression is a valid numeric expression
            if (expr is LiteralExpression)
            {
                return ((LiteralExpression)expr).Value is double;
            }
            else if (expr is BinaryExpression)
            {
                return IsNumericExpression(((BinaryExpression)expr).Left) &&
                       IsNumericExpression(((BinaryExpression)expr).Right);
            }
            else if (expr is UnaryExpression)
            {
                return IsNumericExpression(((UnaryExpression)expr).Right);
            }
            else if (expr is GroupingExpression)
            {
                return IsNumericExpression(((GroupingExpression)expr).Expression);
            }

            return false; // If it's anything else, it's not a numeric expression
        }



        private List<Stmt> Block()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Statement());
            }

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después del bloque.");
            Consume(TokenType.SEMICOLON, "Se esperaba punto y coma ';' después del bloque.");
            return statements;
        }
        private Stmt Statement()
        {
            if (Match(TokenType.LEFT_BRACE))
                return new Block(Block());

            if (Match(TokenType.IF))
                return IfStatement();

            if (Match(TokenType.WHILE))
                return WhileStatement();

            if (Match(TokenType.FOR))
                return ForStatement();


            return ExpressionStatement();
        }

        // Método WhileStatement integrado
        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expression condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
            Stmt body = Statement();
            return new While(condition, body);
        }

        private Stmt ForStatement()
        {
            Token iterator = Consume(TokenType.IDENTIFIER, "Se esperaba un nombre de variable para el iterador.");
            Consume(TokenType.IN, "Se esperaba 'in' después del nombre del iterador.");
            Token iterable = Consume(TokenType.IDENTIFIER, "Se esperaba un nombre de variable para la lista de iteración.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de la expresión de iteración.");

            List<Stmt> body = Block();

            return new For(iterator, iterable, body);
        }


        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expression condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");
            Stmt thenBranch = Statement();
            Stmt elseBranch = null;
            if (Match(TokenType.ELSE))
            {
                elseBranch = Statement();
            }
            return new If(condition, thenBranch, elseBranch);
        }

        private Stmt ExpressionStatement()
        {
            Expression expr = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Expr(expr);
        }

        private class ParseError : Exception { }

        private Expression Expression()
        {
            return Assignment();
        }

        private Expression Assignment()
        {
            Expression expr = Or();

            if (Match(TokenType.ASSIGN, TokenType.PLUS_EQUAL, TokenType.MINUS_EQUAL))
            {
                Token equals = Previous();
                Expression value = Assignment();

                if (expr is Variable variableExpr)
                {
                    Token name = variableExpr.name;

                    if (equals.Type == TokenType.PLUS_EQUAL)
                    {
                        value = new BinaryExpression(new Variable(name), new Token(TokenType.PLUS, "+", null, equals.Line), value);
                    }
                    else if (equals.Type == TokenType.MINUS_EQUAL)
                    {
                        value = new BinaryExpression(new Variable(name), new Token(TokenType.MINUS, "-", null, equals.Line), value);
                    }

                    return new AssignExpression(name, value);
                }
                else if (expr is Get getExpr)
                {
                    return new Set(getExpr.Object, getExpr.Name, value);
                }

                Error(equals, "Se esperaba un nombre de variable o una propiedad.");
            }

            return expr;
        }


        // Método Or() que maneja la lógica de los operadores lógicos 'or' y 'and'.
        private Expression Or()
        {
            Expression expr = And();

            while (Match(TokenType.OR))
            {
                Token operatorToken = Previous();
                Expression right = And();
                expr = new LogicalExpression(expr, operatorToken, right);
            }

            return expr;
        }

        // Método And() que maneja la lógica de los operadores lógicos 'and'.
        private Expression And()
        {
            Expression expr = Equality();

            while (Match(TokenType.AND))
            {
                Token operatorToken = Previous();
                Expression right = Equality();
                expr = new LogicalExpression(expr, operatorToken, right);
            }

            return expr;
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
            // Ajustado para usar INCREMENT y DECREMENT en lugar de PLUS_PLUS y MINUS_MINUS
            if (Match(TokenType.NOT_EQUAL, TokenType.MINUS, TokenType.INCREMENT, TokenType.DECREMENT))
            {
                Token operador = Previous();
                Expression right = Unary();
                return new UnaryExpression(operador, right);
            }
            return Postfix();
        }

        private Expression Postfix()
        {
            Expression expr = Call();

            while (Match(TokenType.INCREMENT, TokenType.DECREMENT))
            {
                Token operador = Previous();
                expr = new PostfixExpression(expr, operador);
            }

            return expr;
        }

        private Expression Call()
        {
            Expression expr = Primary();

            // Booleano para verificar si ya se realizó una llamada
            bool hasCalled = false;

            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                {
                    if (hasCalled)
                    {
                        // Lanzamos un error de parseo usando tu manejo de errores
                        throw Error(Peek(), "Las llamadas anidadas no están permitidas.");
                    }

                    expr = FinishCall(expr);
                    hasCalled = true; // Marcamos que ya se ha realizado una llamada
                }
                else if (Match(TokenType.DOT))
                {
                    Token name = Consume(TokenType.IDENTIFIER, "Se esperaba el nombre de la propiedad después de '.'.");
                    expr = new Get(expr, name);
                }
                else
                {
                    break; // Salimos del bucle si no hay más llamadas ni accesos a propiedades
                }
            }

            return expr;
        }

        private Expression FinishCall(Expression callee)
        {
            var arguments = new List<Expression>();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    arguments.Add(Expression());
                }
                while (Match(TokenType.COMMA));
            }

            var paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return new Call(callee, paren, arguments);
        }

        private Expression Primary()
        {
            if (Match(TokenType.BOOLEAN, TokenType.NUMBER, TokenType.STRING))
            {
                return new LiteralExpression(Previous().Literal);
            }

            if (Match(TokenType.IDENTIFIER))
            {
                return new Variable(Previous());
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
