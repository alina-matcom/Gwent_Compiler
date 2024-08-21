using System.Collections;

namespace GwentInterpreters
{
    public class Interpreter : Expression.IVisitor<object>, Stmt.IVisitor
    {
        private bool hadRuntimeError = false;
        private Environment environment = new Environment();
        // Diccionario para almacenar los efectos definidos
        private Dictionary<string, EffectDefinition> effectDefinitions = new Dictionary<string, EffectDefinition>();
        public void Interpret(List<Stmt> statements)
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
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }

        // Implementación de la visita al nodo EffectStmt
        public void VisitEffectStmt(EffectStmt stmt)
        {
            // Creando una nueva definición de efecto y almacenando la acción
            EffectDefinition effect = new EffectDefinition(stmt);

            // Guardando la definición en el diccionario de efectos
            if (effectDefinitions.ContainsKey(stmt.Name))
            {
                throw new RuntimeError(null, $"El efecto '{stmt.Name}' ya está definido.");
            }
            effectDefinitions[stmt.Name] = effect;
        }


        public void VisitIfStmt(If stmt)
        {
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
            // Se crea un nuevo entorno para el bloque, basado en el entorno actual
            ExecuteBlock(stmt.statements, new Environment(environment));
        }

        // Método auxiliar para ejecutar un bloque de sentencias en un entorno específico
        private void ExecuteBlock(List<Stmt> statements, Environment environment)
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
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
        }
        public object VisitCallExpression(Call expr)
        {
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
            object obj = Evaluate(expr.Object);

            // Verificar si es una instancia de Card o Context
            if (obj is Card card)
            {
                return GetPropertyOrMethod(card, expr.Name);
            }
            else if (obj is Context context)
            {
                return GetPropertyOrMethod(context, expr.Name);
            }

            throw new RuntimeError(expr.Name, "Only instances of Card or Context have properties or methods.");
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
            return environment.Get(expr.name);
        }
        public void VisitExprStmt(Expr stmt)
        {
            Evaluate(stmt.expression);
            return;
        }


        public object VisitLogicalExpression(LogicalExpression expr)
        {
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
            return expr.Accept(this);
        }

        public object VisitAssignExpression(AssignExpression expr)
        {
            object value = Evaluate(expr.Value);
            environment.Assign(expr.Name, value);
            return value;
        }
        public object VisitLiteralExpression(LiteralExpression expr)
        {
            return expr.Value;
        }

        public object VisitGroupingExpression(GroupingExpression expr)
        {
            return Evaluate(expr.Expression);
        }

        public object VisitUnaryExpression(UnaryExpression expr)
        {
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
