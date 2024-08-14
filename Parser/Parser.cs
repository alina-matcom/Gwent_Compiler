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
                // Puedes manejar otros tipos de declaraciones si es necesario.

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
            // El token 'EFFECT' ya fue consumido en el método 'Declaration'
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'effect'.");

            string name = Consume(TokenType.IDENTIFIER, "Se esperaba un nombre para el efecto.").Lexeme;

            Dictionary<string, Type> parameters = new Dictionary<string, Type>();

            if (Match(TokenType.PARAMS))
            {
                Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'Params'.");
                while (!Check(TokenType.RIGHT_BRACE))
                {
                    string paramName = Consume(TokenType.IDENTIFIER, "Se esperaba el nombre de un parámetro.").Lexeme;
                    Consume(TokenType.COLON, "Se esperaba ':' después del nombre del parámetro.");
                    Type paramType = ParseType();
                    parameters[paramName] = paramType;

                    if (!Match(TokenType.COMMA))
                    {
                        break;
                    }
                }
                Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después de la declaración de parámetros.");
            }

            Consume(TokenType.ACTION, "Se esperaba la palabra clave 'Action'.");
            ActionStmt action = ParseAction();

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después de la declaración del efecto.");

            return new EffectStmt(name, parameters, action);
        }
        private ActionStmt ParseAction()
        {
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' para el bloque de acción.");
            Expression targets = Expression(); // Asumimos que tienes una forma de parsear 'targets'
            List<Stmt> body = Block(); // Reutilizamos el método Block para el cuerpo de la acción
            return new ActionStmt(targets, body);
        }
        private Type ParseType()
        {
            if (Match(TokenType.NUMBER)) return typeof(int);
            if (Match(TokenType.STRING)) return typeof(string);
            if (Match(TokenType.BOOLEAN)) return typeof(bool);
            throw Error(Peek(), "Tipo no válido.");
        }

        private Stmt CardDeclaration()
        {
            Consume(TokenType.CARD, "Se esperaba la palabra clave 'card'.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'card'.");

            // Parsear propiedades de la carta
            string type = ParseStringProperty("Type");
            string name = ParseStringProperty("Name");
            string faction = ParseStringProperty("Faction");
            int power = ParseIntProperty("Power");
            List<string> range = ParseRange();

            // Parsear OnActivation
            List<OnActivationStmt> onActivation = new List<OnActivationStmt>();
            if (Match(TokenType.ONACTIVATION))
            {
                Consume(TokenType.COLON, "Se esperaba ':' después de 'OnActivation'.");
                Consume(TokenType.LEFT_BRACKET, "Se esperaba '[' después de 'OnActivation:'.");

                while (!Check(TokenType.RIGHT_BRACKET))
                {
                    onActivation.Add(ParseOnActivation());
                    if (!Match(TokenType.COMMA))
                    {
                        break;
                    }
                }

                Consume(TokenType.RIGHT_BRACKET, "Se esperaba ']' después de la lista de 'OnActivation'.");
            }

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después de la declaración de la carta.");

            return new CardStmt(type, name, faction, power, range, onActivation);
        }

        private string ParseStringProperty(string propertyName)
        {
            Consume(TokenType.IDENTIFIER, $"Se esperaba '{propertyName}'.");
            Consume(TokenType.COLON, "Se esperaba ':' después del nombre de la propiedad.");
            return Consume(TokenType.STRING, $"Se esperaba un valor para '{propertyName}'.").Lexeme;
        }

        private int ParseIntProperty(string propertyName)
        {
            Consume(TokenType.IDENTIFIER, $"Se esperaba '{propertyName}'.");
            Consume(TokenType.COLON, "Se esperaba ':' después del nombre de la propiedad.");
            return int.Parse(Consume(TokenType.NUMBER, $"Se esperaba un valor para '{propertyName}'.").Lexeme);
        }

        private List<string> ParseRange()
        {
            Consume(TokenType.RANGE, "Se esperaba 'Range'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Range'.");
            Consume(TokenType.LEFT_BRACKET, "Se esperaba '[' para la lista de rangos.");

            List<string> range = new List<string>();
            while (!Check(TokenType.RIGHT_BRACKET))
            {
                range.Add(Consume(TokenType.STRING, "Se esperaba un valor de rango.").Lexeme);
                if (!Match(TokenType.COMMA))
                {
                    break;
                }
            }

            Consume(TokenType.RIGHT_BRACKET, "Se esperaba ']' después de la lista de rangos.");
            return range;
        }

        private OnActivationStmt ParseOnActivation()
        {
            // Parsear el efecto
            EffectStmt effect = ParseEffect();

            // Parsear el selector
            SelectorStmt selector = ParseSelector();

            // Parsear el postAction si está presente
            PostActionStmt postAction = null;
            if (Match(TokenType.POST_ACTION))
            {
                postAction = ParsePostAction();
            }

            return new OnActivationStmt(effect, selector, postAction);
        }


        private Stmt Statement()
        {
            if (Match(TokenType.LEFT_BRACE))
                return new Block(Block());

            if (Match(TokenType.IF))
                return IfStatement();

            if (Match(TokenType.WHILE))
                return WhileStatement();

            return ExpressionStatement();
        }
        private List<Stmt> Block()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Statement());
            }

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después del bloque.");
            return statements;
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
            Consume(TokenType.FOR, "Se esperaba 'for'.");
            Token iterator = Consume(TokenType.IDENTIFIER, "Se esperaba un nombre de variable para el iterador.");
            Consume(TokenType.IN, "Se esperaba 'in' después del nombre del iterador.");
            Expression iterable = Expression();
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
            // Llamamos a Or() en lugar de Equality() para integrar la lógica adicional.
            Expression expr = Or();

            if (Match(TokenType.ASSIGN))
            {
                Token equals = Previous();
                Expression value = Assignment();

                if (expr is Variable variableExpr)
                {
                    Token name = variableExpr.name;
                    return new AssignExpression(name, value);
                }

                // Error: Se esperaba un nombre de variable a la izquierda del '='
                Error(equals, "Se esperaba un nombre de variable.");
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
