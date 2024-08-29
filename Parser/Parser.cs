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
                if (Match(TokenType.EFFECT_DECLARATION)) return EffectDeclaration();
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
            Console.WriteLine("Inicio de la declaración de efecto"); // Log
            // Consumimos la apertura del bloque '{'
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'effect'.");

            // Parseo del nombre del efecto
            string name = ParseStringAttribute(TokenType.IDENTIFIER, "Name");
            Console.WriteLine($"Nombre del efecto: {name}"); // Log

            // Inicialización de la lista de parámetros (opcional)
            List<Parameter> parameters = new();

            // Verificamos si 'Params' está presente
            if (Match(TokenType.PARAMS))
            {
                Consume(TokenType.COLON, "Se esperaba ':' después de 'Params'.");
                parameters = ParseParams();
                Console.WriteLine($"Parámetros del efecto: {string.Join(", ", parameters)}"); // Log
                // Consumimos la coma después de los parámetros
                Consume(TokenType.COMMA, "Se esperaba ',' después del bloque de parámetros.");
            }

            // Parseo de la acción
            Consume(TokenType.ACTION, "Se esperaba la clave 'Action' dentro del efecto.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Action'.");
            Action action = ParseAction();
            Console.WriteLine($"Acción del efecto: {action}"); // Log

            // Consumimos la clausura del bloque '}'
            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración de efecto.");
            Console.WriteLine("Fin de la declaración de efecto"); // Log
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

        // Función de logging
        private void Log(string message)
        {
            Console.WriteLine($"[LOG] {message}");
        }

        private Stmt CardDeclaration()
        {
            Log("Iniciando el parseo de una declaración de carta.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de 'card'.");

            // Parseo de los atributos de la carta
            string type = ParseStringAttribute(TokenType.IDENTIFIER, "Type");
            Log($"Tipo de carta: {type}");
            string name = ParseStringAttribute(TokenType.IDENTIFIER, "Name");
            Log($"Nombre de carta: {name}");
            string faction = ParseStringAttribute(TokenType.IDENTIFIER, "Faction");
            Log($"Facción de carta: {faction}");
            Expression power = ParseExpressionAttribute(TokenType.IDENTIFIER, "Power");
            Log($"Poder de carta: {power}");
            Consume(TokenType.COMMA, "Se esperaba ',' después del valor de 'Power'.");
            List<string> range = ParseStringListAttribute(TokenType.IDENTIFIER, "Range");
            Log($"Rango de carta: {string.Join(", ", range)}");
            Consume(TokenType.COMMA, "Se esperaba ',' después del valor de 'Range'.");

            // Validar los rangos
            var validRanges = new HashSet<string> { "Melee", "Ranged", "Siege" };
            foreach (var r in range)
            {
                if (!validRanges.Contains(r))
                {
                    throw Error(Peek(), $"Rango inválido en la definición de carta: {r}. Debe ser uno de: {string.Join(", ", validRanges)}");
                }
            }

            // Parseo de la lista de efectos en OnActivation
            Consume(TokenType.ONACTIVATION, "Se esperaba la clave 'OnActivation'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'OnActivation'.");
            List<EffectAction> onActivation = ParseEffects();
            Log($"Efectos de OnActivation: {onActivation.Count} efectos parseados.");

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración de carta.");

            Log("Finalizando el parseo de una declaración de carta.");
            return new CardStmt(type, name, faction, power, range, onActivation);
        }

        private List<EffectAction> ParseEffects()
        {
            List<EffectAction> effects = new List<EffectAction>();

            Consume(TokenType.LEFT_BRACKET, "Se esperaba '[' después de 'OnActivation'.");
            while (!Check(TokenType.RIGHT_BRACKET) && !IsAtEnd())
            {
                effects.Add(ParseEffect()); // Ahora devuelve un EffectAction
                if (!Check(TokenType.RIGHT_BRACKET))
                {
                    Consume(TokenType.COMMA, "Se esperaba ',' entre efectos.");
                }
            }
            Consume(TokenType.RIGHT_BRACKET, "Se esperaba ']' al final de la lista de efectos.");

            Log($"Parseados {effects.Count} efectos.");
            return effects;
        }


        private EffectAction ParseEffect(Selector parentSelector = null, bool isPostAction = false)
        {
            Log("Iniciando el parseo de un efecto.");
            Consume(TokenType.LEFT_BRACE, "Se esperaba '{' al comienzo de un efecto.");

            // Parseo de la invocación del efecto.
            Consume(TokenType.EFFECT_CALL, "Se esperaba la clave 'Effect'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Effect'.");
            EffectInvocation effect = ParseEffectInvocation();
            Log($"Efecto invocado: {effect.Name}");

            // Parseo del selector.
            Selector selector = null;
            if (Match(TokenType.SELECTOR))
            {
                Consume(TokenType.COLON, "Se esperaba ':' después de 'Selector'.");
                Consume(TokenType.LEFT_BRACE, "Se esperaba '{' después de ':' en 'Selector'.");
                selector = ParseSelector(isPostAction);
                Log($"Selector parseado: {selector.Source}");
            }
            else if (parentSelector != null)
            {
                // Usar el selector del efecto padre si no se especifica uno en el PostAction.
                selector = parentSelector;
                Log("Usando el selector del efecto padre.");
            }
            else
            {
                throw Error(Peek(), "Se esperaba un 'Selector' o un 'parentSelector' en la declaración de PostAction.");
            }

            // Parseo de la PostAction (opcional).
            EffectAction postAction = null;
            if (Match(TokenType.POSTACTION))
            {
                Consume(TokenType.COLON, "Se esperaba ':' después de 'PostAction'.");
                postAction = ParseEffect(selector, true); // Aquí pasamos true indicando que es un PostAction
                Log("PostAction parseado.");
            }

            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración de efecto.");

            Log("Finalizando el parseo de un efecto.");
            return new EffectAction(effect, selector, postAction);
        }
        private EffectInvocation ParseEffectInvocation()
        {
            Log("Iniciando el parseo de una invocación de efecto.");
            string name;
            Dictionary<string, Expression> parameters = new Dictionary<string, Expression>();

            if (Check(TokenType.STRING))
            {
                // Cuando el nombre del efecto está dado directamente como una cadena
                name = Consume(TokenType.STRING, "Se esperaba el nombre del efecto como cadena.").Lexeme;
                Log($"Nombre del efecto: {name}");
            }
            else
            {
                // Cuando el nombre del efecto y los parámetros están entre llaves
                Consume(TokenType.LEFT_BRACE, "Se esperaba '{' en la declaración del efecto.");
                name = ParseStringAttribute(TokenType.IDENTIFIER, "Name");
                Log($"Nombre del efecto: {name}");

                while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
                {
                    string paramName = Peek().Lexeme; // Obtener el nombre del parámetro
                    Advance(); // Avanzar al siguiente token
                    Consume(TokenType.COLON, $"Se esperaba ':' después de '{paramName}'.");

                    // Aquí se debe parsear el valor del parámetro
                    Expression paramValue = Expression(); // Suponiendo que 'Expression()' maneja la expresión del valor del parámetro
                    parameters[paramName] = paramValue;
                    Log($"Parámetro: {paramName} = {paramValue}");

                    // Consumir ',' si no es el final del bloque de parámetros
                    if (!Check(TokenType.RIGHT_BRACE))
                    {
                        Consume(TokenType.COMMA, "Se esperaba ',' entre parámetros.");
                    }
                }

                Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' al final de la declaración del efecto.");
                Consume(TokenType.COMMA, "Se esperaba ',' después de '}' en la invocación del efecto.");
            }

            Log("Finalizando el parseo de una invocación de efecto.");
            return new EffectInvocation(name, parameters);
        }

        private Selector ParseSelector(bool isPostAction)
        {
            Log("Iniciando el parseo de un selector.");
            // Se espera que siempre haya un 'Source'.
            string source = ParseStringAttributeNew(TokenType.SOURCE, "Source");
            Log($"Source del selector: {source}");

            // Validar el Source
            var validSources = new HashSet<string> { "hand", "otherHand", "deck", "otherDeck", "field", "otherField", "parent", "board" };
            if (!validSources.Contains(source))
            {
                throw Error(Peek(), $"Source inválido: {source}. Debe ser uno de: {string.Join(", ", validSources)}");
            }

            // Verifica si el source es "parent" y si está siendo usado en un PostAction
            if (source == "parent" && !isPostAction)
            {
                throw Error(Peek(), "'parent' solo puede ser usado como 'source' en un 'PostAction'.");
            }

            Consume(TokenType.SINGLE, "Se esperaba la clave 'Single'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Single'.");
            bool single = Consume(TokenType.BOOLEAN, "Se esperaba un valor booleano para 'Single'.").Literal.Equals(true);
            Log($"Single del selector: {single}");
            Consume(TokenType.COMMA, "Se esperaba ',' después de 'Single'.");

            Consume(TokenType.PREDICATE, "Se esperaba la clave 'Predicate'.");
            Consume(TokenType.COLON, "Se esperaba ':' después de 'Predicate'.");

            Predicate predicate = ParsePredicate();
            Consume(TokenType.RIGHT_BRACE, "Se esperaba '}' después de 'Predicate'.");
            Consume(TokenType.COMMA, "Se esperaba ',' después de '}'.");
            Log("Finalizando el parseo de un selector.");

            return new Selector(source, single, predicate);
        }

        private Predicate ParsePredicate()
        {
            Log("Iniciando el parseo de un predicado.");
            // Parseamos la expresión del predicado como una función lambda
            Consume(TokenType.LEFT_PAREN, "Se esperaba '(' al inicio del predicado.");
            var parameter = Consume(TokenType.IDENTIFIER, "Se esperaba un parámetro para el predicado.");
            Log($"Parámetro del predicado: {parameter.Lexeme}");
            Consume(TokenType.RIGHT_PAREN, "Se esperaba ')' después del parámetro del predicado.");
            Consume(TokenType.LAMBDA, "Se esperaba '=>' después del parámetro del predicado.");
            var body = Expression();
            Log("Finalizando el parseo de un predicado.");

            return new Predicate(parameter, body);
        }

        private string ParseStringAttribute(TokenType expectedTokenType, string attributeName)
        {
            Token identifierToken = Consume(TokenType.IDENTIFIER, $"Se esperaba la clave '{attributeName}'.");
            if (identifierToken.Lexeme != attributeName)
            {
                throw Error(identifierToken, $"Se esperaba la clave '{attributeName}', pero se encontró '{identifierToken.Lexeme}'.");
            }
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");
            Token stringToken = Consume(TokenType.STRING, $"Se esperaba un valor de cadena para '{attributeName}'.");
            Consume(TokenType.COMMA, $"Se esperaba ',' después del valor de cadena para '{attributeName}'.");
            return stringToken.Lexeme;
        }

        private string ParseStringAttributeNew(TokenType expectedTokenType, string attributeName)
        {
            Consume(expectedTokenType, $"Se esperaba la clave '{attributeName}'.");
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");
            Token stringToken = Consume(TokenType.STRING, $"Se esperaba un valor de cadena para '{attributeName}'.");
            Consume(TokenType.COMMA, $"Se esperaba ',' después del valor de cadena para '{attributeName}'.");
            return (string)stringToken.Literal; // Usar el valor literal en lugar del lexema
        }

        private List<string> ParseStringListAttribute(TokenType expectedTokenType, string attributeName)
        {
            Token identifierToken = Consume(TokenType.IDENTIFIER, $"Se esperaba la clave '{attributeName}'.");
            if (identifierToken.Lexeme != attributeName)
            {
                throw Error(identifierToken, $"Se esperaba la clave '{attributeName}', pero se encontró '{identifierToken.Lexeme}'.");
            }
            Consume(TokenType.COLON, $"Se esperaba ':' después de '{attributeName}'.");
            Consume(TokenType.LEFT_BRACKET, $"Se esperaba '[' después de ':' en '{attributeName}'.");

            List<string> values = new List<string>();
            do
            {
                Token stringToken = Consume(TokenType.STRING, $"Se esperaba un valor de cadena en la lista de '{attributeName}'.");
                values.Add((string)stringToken.Literal); // Usar el valor literal en lugar del lexema
            } while (Match(TokenType.COMMA));

            Consume(TokenType.RIGHT_BRACKET, $"Se esperaba ']' al final de la lista de '{attributeName}'.");

            return values;
        }

        private Expression ParseExpressionAttribute(TokenType expectedTokenType, string attributeName)
        {
            Token identifierToken = Consume(TokenType.IDENTIFIER, $"Se esperaba la clave '{attributeName}'.");
            if (identifierToken.Lexeme != attributeName)
            {
                throw Error(identifierToken, $"Se esperaba la clave '{attributeName}', pero se encontró '{identifierToken.Lexeme}'.");
            }
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
                        value = new BinaryExpression(new Variable(name), new Token(TokenType.PLUS, "+", null, equals.Location), value);
                    }
                    else if (equals.Type == TokenType.MINUS_EQUAL)
                    {
                        value = new BinaryExpression(new Variable(name), new Token(TokenType.MINUS, "-", null, equals.Location), value);
                    }

                    return new AssignExpression(name, value);
                }
                else if (expr is Get getExpr)
                {
                    if (equals.Type == TokenType.PLUS_EQUAL)
                    {
                        value = new BinaryExpression(getExpr, new Token(TokenType.PLUS, "+", null, equals.Location), value);
                    }
                    else if (equals.Type == TokenType.MINUS_EQUAL)
                    {
                        value = new BinaryExpression(getExpr, new Token(TokenType.MINUS, "-", null, equals.Location), value);
                    }

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
                    case TokenType.EFFECT_DECLARATION:
                    case TokenType.CARD:
                        return;

                }

                Advance();
            }
        }


    }
}