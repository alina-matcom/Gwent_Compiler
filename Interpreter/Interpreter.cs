using System.Collections;

namespace GwentInterpreters
{
    public class Interpreter : Expression.IVisitor<object>, Stmt.IVisitor
    {
        private bool hadRuntimeError = false;
        internal Environment environment = new Environment();
        // Diccionario para almacenar los efectos definidos
        private Dictionary<string, EffectDefinition> effectDefinitions = new Dictionary<string, EffectDefinition>();
        // Lista para almacenar las cartas
        private List<Card> cards = new List<Card>();
        public List<Card> Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (RuntimeError error)
            {
                RuntimeError(error);
            }
            // Devuelve la lista de cartas procesadas.
            return cards;
        }


        private void Execute(Stmt stmt)
        {
            Console.WriteLine($"Executing statement: {stmt}");
            stmt.Accept(this);
        }

        // Implementación de la visita al nodo EffectStmt
        public void VisitEffectStmt(EffectStmt stmt)
        {
            Console.WriteLine($"Visiting EffectStmt: {stmt.Name}");
            // Obtener la lista de parámetros del EffectStmt
            List<Parameter> parameters = stmt.Params;

            // Visitar la expresión de acción para obtener la instancia de ActionFunction
            ActionFunction actionFunction = (ActionFunction)VisitActionExpression(stmt.Action);


            // Crear la nueva definición de efecto con los parámetros y la función de acción
            EffectDefinition effect = new EffectDefinition(parameters, actionFunction);

            // Guardar la definición en el diccionario de efectos
            if (effectDefinitions.ContainsKey(stmt.Name))
            {
                throw new RuntimeError(null, $"El efecto '{stmt.Name}' ya está definido.");
            }
            effectDefinitions[stmt.Name] = effect;
        }

        public void VisitCardStmt(CardStmt stmt)
        {
            Console.WriteLine($"Visiting CardStmt: {stmt.Name}");
            // Extraer y evaluar los atributos de la carta
            string type = stmt.Type;
            string name = stmt.Name;
            string faction = stmt.Faction;

            double power = Evaluate(stmt.Power) as double? ?? throw new RuntimeError(null, "La evaluación de Power no devolvió un número.");

            // Obtener la lista de rangos
            List<string> range = stmt.Range;

            // Convertir la lista de EffectAction a EffectActionResult
            List<EffectActionResult> onActivationResults = new List<EffectActionResult>();
            foreach (var effectAction in stmt.OnActivation)
            {
                // Evaluar cada EffectAction y agregar el resultado a la lista
                var result = (EffectActionResult)VisitEffectAction(effectAction);
                onActivationResults.Add(result);
            }

            // Crear una nueva instancia de la carta con los resultados evaluados
            Card newCard = new Card(type, name, faction, power, range, onActivationResults);

            // Agregar la carta a la lista de cartas
            cards.Add(newCard);

        }



        public object VisitSelectorExpr(Selector expr)
        {
            Console.WriteLine($"Visiting SelectorExpr: {expr}");
            // Evaluar el Predicate
            var predicateFunction = (Func<Card, bool>)VisitPredicate(expr.Predicate);

            // Crear y devolver la instancia de SelectorResult
            return new SelectorResult(expr.Source, expr.Single, predicateFunction);
        }

        public object VisitPredicate(Predicate predicate)
        {
            Console.WriteLine($"Visiting Predicate: {predicate}");
            // Devolver una lambda que acepte una Card y devuelva un bool
            return new Func<Card, bool>(card =>
            {
                // Crear un nuevo intérprete temporal
                Interpreter tempInterpreter = new Interpreter();

                // Crear un entorno temporal basado en el entorno actual
                Environment tempEnv = new Environment(this.environment);

                // Definir la variable del parámetro (que es la carta) en el nuevo entorno
                tempEnv.Define(predicate.Parameter.Lexeme, card);

                // Establecer el entorno temporal en el intérprete temporal
                tempInterpreter.environment = tempEnv;

                // Evaluar el cuerpo del predicado en el intérprete temporal
                var result = tempInterpreter.Evaluate(predicate.Body) as bool?;

                // Si el resultado no es booleano, lanzar un error
                if (result == null)
                {
                    throw new RuntimeError(null, "El predicado no evaluó a un valor booleano.");
                }

                // Devolver el resultado booleano
                return result.Value;
            });
        }



        public object VisitEffectInvocationExpr(EffectInvocation expr)
        {
            Console.WriteLine($"Visiting EffectInvocationExpr: {expr.Name}");
            // 1. Verificar si el efecto está definido en el diccionario.
            if (!effectDefinitions.ContainsKey(expr.Name))
            {
                throw new RuntimeError(null, $"El efecto '{expr.Name}' no está definido.");
            }

            // 2. Obtener la definición del efecto.
            var effectDef = effectDefinitions[expr.Name];

            // 3. Evaluar las expresiones en los parámetros y almacenarlas en un nuevo diccionario.
            var evaluatedParams = new Dictionary<string, object>();
            foreach (var param in expr.Parameters)
            {
                evaluatedParams[param.Key] = Evaluate(param.Value);
            }

            // 4. Verificar que la cantidad de parámetros coincida con la definición.
            if (effectDef.Parameters.Count != evaluatedParams.Count)
            {
                throw new RuntimeError(null,
                    $"Error de aridad en el efecto '{expr.Name}': se esperaban {effectDef.Parameters.Count} parámetros, pero se recibieron {evaluatedParams.Count}.");
            }

            // 5. Verificar que los tipos coincidan.
            foreach (var parameter in effectDef.Parameters)
            {
                if (!evaluatedParams.ContainsKey(parameter.Name.Lexeme))
                {
                    throw new RuntimeError(null,
                        $"El parámetro '{parameter.Name.Lexeme}' no fue proporcionado en la invocación del efecto '{expr.Name}'.");
                }

                var value = evaluatedParams[parameter.Name.Lexeme];

                // Verificación del tipo basado en el tipo especificado en `parameter.Type`.
                switch (parameter.Type.Type)
                {
                    case TokenType.NUMBER_SPECIFIER:
                        if (!(value is double))
                        {
                            throw new RuntimeError(null,
                                $"El parámetro '{parameter.Name.Lexeme}' en el efecto '{expr.Name}' debería ser de tipo 'number', pero se recibió '{value.GetType()}'.");
                        }
                        break;

                    case TokenType.STRING_SPECIFIER:
                        if (!(value is string))
                        {
                            throw new RuntimeError(null,
                                $"El parámetro '{parameter.Name.Lexeme}' en el efecto '{expr.Name}' debería ser de tipo 'string', pero se recibió '{value.GetType()}'.");
                        }
                        break;

                    case TokenType.BOOLEAN_SPECIFIER:
                        if (!(value is bool))
                        {
                            throw new RuntimeError(null,
                                $"El parámetro '{parameter.Name.Lexeme}' en el efecto '{expr.Name}' debería ser de tipo 'boolean', pero se recibió '{value.GetType()}'.");
                        }
                        break;

                    default:
                        throw new RuntimeError(null,
                            $"Tipo de parámetro desconocido '{parameter.Type.Type}' en la definición del efecto '{expr.Name}'.");
                }
            }

            // 6. Crear y devolver una nueva instancia de EffectInstance.
            return new EffectInstance(expr.Name, evaluatedParams, effectDef.ActionFunction);
        }
        public object VisitEffectAction(EffectAction effectAction)
        {
            Console.WriteLine($"Visiting EffectAction: {effectAction}");
            // Evaluar el EffectInvocation y obtener el resultado.
            var effectInstance = (EffectInstance)VisitEffectInvocationExpr(effectAction.Effect);

            // Evaluar el SelectorExpr y obtener el resultado.
            var selectorResult = (SelectorResult)VisitSelectorExpr(effectAction.Selector);

            // Evaluar el PostAction recursivamente, si existe.
            EffectActionResult postActionResult = null;
            if (effectAction.PostAction != null)
            {
                postActionResult = (EffectActionResult)VisitEffectAction(effectAction.PostAction);
            }

            // Crear y devolver una instancia de EffectActionResult.
            return new EffectActionResult(effectInstance, selectorResult, postActionResult);
        }



        public object VisitActionExpression(Action expr)
        {
            Console.WriteLine($"Visiting ActionExpression: {expr}");
            // Crear una nueva instancia de ActionFunction usando la declaración de acción.
            return new ActionFunction(expr);
        }


        public void VisitIfStmt(If stmt)
        {
            Console.WriteLine($"Visiting IfStmt: {stmt}");
            if (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                Execute(stmt.elseBranch);
            }
        }
        // Método para visitar y ejecutar un bloque de sentencias
        public void VisitBlockStmt(Block stmt)
        {
            Console.WriteLine($"Visiting BlockStmt");
            // Se crea un nuevo entorno para el bloque, basado en el entorno actual
            ExecuteBlock(stmt.statements, new Environment(environment));
        }

        // Método auxiliar para ejecutar un bloque de sentencias en un entorno específico
        internal void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            // Guardamos el entorno anterior para poder restaurarlo más tarde
            Environment previous = this.environment;
            try
            {
                // Cambiamos el entorno actual al nuevo entorno creado para el bloque
                this.environment = environment;

                // Ejecutamos cada sentencia en el nuevo entorno
                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                // Restauramos el entorno original una vez que todas las sentencias se han ejecutado
                this.environment = previous;
            }
        }

        public void VisitForStmt(For stmt)
        {
            Console.WriteLine($"Visiting ForStmt: {stmt}");
            // Evalúa la expresión iterable y asegúrate de que es una colección.
            object iterable = environment.Get(stmt.Iterable);

            if (iterable is Iterable collection)
            {
                // Itera sobre la colección.
                foreach (var item in collection)
                {
                    // Crea un nuevo entorno para esta iteración.
                    var localEnvironment = new Environment(environment);

                    // Define el 'target' solo en este entorno local.
                    localEnvironment.Define(stmt.Iterator.Lexeme, item);

                    // Ejecuta el cuerpo del bucle en el entorno local.
                    ExecuteBlock(stmt.Body, localEnvironment);
                }
            }
            else
            {
                throw new RuntimeError(stmt.Iterator, "La expresión no es iterable.");
            }
        }


        public void VisitWhileStmt(While stmt)
        {
            Console.WriteLine($"Visiting WhileStmt: {stmt}");
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
        }
        public object VisitCallExpression(Call expr)
        {
            Console.WriteLine($"Visiting CallExpression: {expr}");
            // Evaluar el objeto que contiene el método
            object callee = Evaluate(expr.Callee);

            // Verificar si es un método callable
            if (callee is CallableMethod callableMethod)
            {
                // Evaluar los argumentos
                var arguments = new List<object>();
                foreach (var argument in expr.Arguments)
                {
                    arguments.Add(Evaluate(argument));
                }

                // Validar los argumentos
                if (!callableMethod.CanInvoke(arguments, out string errorMessage))
                {
                    throw new RuntimeError(expr.Paren, errorMessage);
                }

                // Llamar al método
                return callableMethod.Call(arguments);
            }

            // Usar el token Paren en el RuntimeError si no es un método callable
            throw new RuntimeError(expr.Paren, "Solo se pueden llamar métodos.");
        }

        public object VisitGetExpression(Get expr)
        {
            Console.WriteLine($"Visiting GetExpression: {expr}");
            object obj = Evaluate(expr.Object);

            // Verificar si es una instancia de Card, Context o Iterable
            if (obj is Card card)
            {
                return GetPropertyOrMethod(card, expr.Name);
            }
            else if (obj is Context context)
            {
                return GetPropertyOrMethod(context, expr.Name);
            }
            else if (obj is Iterable iterable)
            {
                return GetPropertyOrMethod(iterable, expr.Name);
            }

            throw new RuntimeError(expr.Name, "Only instances of Card, Context, or Iterable have properties or methods.");
        }


        private object GetPropertyOrMethod(object obj, Token name)
        {
            // Obtener la propiedad
            var property = obj.GetType().GetProperty(name.Lexeme);
            if (property != null)
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    return value;
                }
            }

            // Obtener el método
            var method = obj.GetType().GetMethod(name.Lexeme);
            if (method != null)
            {
                return new CallableMethod(obj, method);
            }

            throw new RuntimeError(name, $"Undefined property or method '{name.Lexeme}'.");
        }

        public object VisitSetExpression(Set expr)
        {

            Console.WriteLine($"Visiting SetExpression: {expr}");
            object obj = Evaluate(expr.Object);
            if (!(obj is Card) && !(obj is Context))
            {
                throw new RuntimeError(expr.Name, "Solo las instancias de Card o Context tienen campos.");
            }

            object value = Evaluate(expr.Value);
            return SetProperty(obj, expr.Name, value);
        }
        private object SetProperty(object obj, Token name, object value)
        {
            var property = obj.GetType().GetProperty(name.Lexeme);
            if (property != null)
            {
                property.SetValue(obj, value);
                return value;
            }
            throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
        }
        public object VisitVariableExpr(Variable expr)
        {
            Console.WriteLine($"Visiting VariableExpr: {expr}");
            return environment.Get(expr.name);
        }
        public void VisitExprStmt(Expr stmt)
        {
            Console.WriteLine($"Visiting ExprStmt: {stmt}");
            Evaluate(stmt.expression);
            return;
        }


        public object VisitLogicalExpression(LogicalExpression expr)
        {
            Console.WriteLine($"Visiting LogicalExpression: {expr}");
            object left = Evaluate(expr.Left);
            if (expr.Operator.Type == TokenType.OR)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.Right);
        }

        // Método auxiliar para evaluar expresiones
        private object Evaluate(Expression expr)
        {
            Console.WriteLine($"Evaluating expression: {expr}");
            return expr.Accept(this);
        }

        public object VisitAssignExpression(AssignExpression expr)
        {
            Console.WriteLine($"Visiting AssignExpression: {expr}");
            object value = Evaluate(expr.Value);
            environment.Assign(expr.Name, value);
            return value;
        }
        public object VisitLiteralExpression(LiteralExpression expr)
        {
            Console.WriteLine($"Visiting LiteralExpression: {expr}");
            return expr.Value;
        }

        public object VisitGroupingExpression(GroupingExpression expr)
        {
            Console.WriteLine($"Visiting GroupingExpression: {expr}");
            return Evaluate(expr.Expression);
        }

        public object VisitUnaryExpression(UnaryExpression expr)
        {
            Console.WriteLine($"Visiting UnaryExpression: {expr}");
            object right = Evaluate(expr.Right);
            switch (expr.Operator.Type)
            {
                case TokenType.NOT:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    CheckNumberOperand(expr.Operator, right);
                    return -(double)right;
                case TokenType.INCREMENT: // Ajustado para usar INCREMENT
                    return Increment(expr.Operator, expr.Right, true);
                case TokenType.DECREMENT: // Ajustado para usar DECREMENT
                    return Decrement(expr.Operator, expr.Right, true);
            }
            // Inalcanzable
            return null;
        }

        public object VisitPostfixExpression(PostfixExpression expr)
        {
            Console.WriteLine($"Visiting PostfixExpression: {expr}");
            switch (expr.Operator.Type)
            {
                case TokenType.INCREMENT: // Ajustado para usar INCREMENT
                    return Increment(expr.Operator, expr.Left, false);
                case TokenType.DECREMENT: // Ajustado para usar DECREMENT
                    return Decrement(expr.Operator, expr.Left, false);
            }
            // Inalcanzable
            return null;
        }
        private object Increment(Token _operator, Expression expr, bool isPrefix)
        {
            Console.WriteLine($"Incrementing: {expr}");
            if (expr is Variable variableExpr)
            {
                object value = environment.Get(variableExpr.name);
                CheckNumberOperand(_operator, value);
                double newValue = (double)value + 1;
                environment.Assign(variableExpr.name, newValue);
                return isPrefix ? newValue : value;
            }
            throw new RuntimeError(_operator, "El operando debe ser una variable.");
        }

        private object Decrement(Token _operator, Expression expr, bool isPrefix)
        {

            Console.WriteLine($"Decrementing: {expr}");
            if (expr is Variable variableExpr)
            {
                object value = environment.Get(variableExpr.name);
                CheckNumberOperand(_operator, value);
                double newValue = (double)value - 1;
                environment.Assign(variableExpr.name, newValue);
                return isPrefix ? newValue : value;
            }
            throw new RuntimeError(_operator, "El operando debe ser una variable.");
        }
        public object VisitBinaryExpression(BinaryExpression expr)
        {
            Console.WriteLine($"Visiting BinaryExpression: {expr}");
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);
            switch (expr.Operator.Type)
            {
                case TokenType.MINUS:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left - (double)right;

                case TokenType.PLUS:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left + (double)right;

                case TokenType.SLASH:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left / (double)right;

                case TokenType.STAR:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left * (double)right;

                case TokenType.GREATER:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left > (double)right;

                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left >= (double)right;

                case TokenType.LESS:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left < (double)right;

                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left <= (double)right;

                case TokenType.NOT_EQUAL:
                    return !IsEqual(left, right);

                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
            }
            // Inalcanzable
            return null;
        }

        private void CheckNumberOperand(Token _operator, object operand)
        {
            if (operand is double) return;
            throw new RuntimeError(_operator, "El operando debe ser un número.");
        }

        private void CheckNumberOperands(Token _operator, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeError(_operator, "Ambos operandos deben ser números.");
        }
        private bool IsTruthy(object value)
        {
            if (value == null) return false;
            if (value is bool) return (bool)value;
            return true;
        }

        // Método auxiliar para verificar la igualdad entre dos objetos
        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        private string Stringify(object obj)
        {
            if (obj == null) return null;
            if (obj is double)
            {
                string text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }
            return obj.ToString();
        }

        // Método para manejar errores en tiempo de ejecución
        private void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine($"{error.Message}\n[file {error.Token.Location.File}, line {error.Token.Location.Line}, column {error.Token.Location.Column}]");
            hadRuntimeError = true;

        }
    }
}
