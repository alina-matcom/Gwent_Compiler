namespace GwentInterpreters
{
    public class Interpreter : IVisitor<object>
    {
        public object VisitLiteralExpression(LiteralExpression expr)
        {
            return expr.Value;
        }

        public object VisitGroupingExpression(GroupingExpression expr)
        {
            return Evaluate(expr.Expression);
        }

        // Método auxiliar para evaluar expresiones
        private object Evaluate(Expression expr)
        {
            return expr.Accept(this);
        }

        public object VisitUnaryExpression(UnaryExpression expr)
        {
            object right = Evaluate(expr.Right);
            switch (expr.Operator.Type)
            {
                case TokenType.NOT:
                    return !IsTruthy(right);
                case TokenType.MINUS:
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
                    return (double)left - (double)right;
                case TokenType.PLUS:
                    if (left is double && right is double)
                        return (double)left + (double)right;
                    if (left is string && right is string)
                        return (string)left + (string)right;
                    throw new InvalidOperationException("Operands must be numbers or strings for '+' operator.");
                case TokenType.SLASH:
                    return (double)left / (double)right;
                case TokenType.STAR:
                    return (double)left * (double)right;
                case TokenType.GREATER:
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    return (double)left <= (double)right;
                case TokenType.NOT_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
            }
            // Inalcanzable
            return null;
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

    }

}
