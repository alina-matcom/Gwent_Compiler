namespace GwentInterpreters
{
    public class Interpreter : Expression.IVisitor<object>, Stmt.IVisitor
    {
        private bool hadRuntimeError = false;
        private Environment environment = new Environment();
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

        public void VisitVarStmt(Var stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
            }
            environment.Define(stmt.name.Lexeme, value);
        }

        public void VisitWhileStmt(While stmt)
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
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
            }
            // Inalcanzable
            return null;
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
