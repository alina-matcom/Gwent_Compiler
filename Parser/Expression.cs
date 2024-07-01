using System;

namespace GwentInterpreters
{
    /// <summary>
    /// Interface for the Visitor pattern.
    /// </summary>
    public interface IVisitor<T>
    {
        T VisitBinaryExpression(BinaryExpression expr);
        T VisitUnaryExpression(UnaryExpression expr);
        T VisitLiteralExpression(LiteralExpression expr);
        T VisitGroupingExpression(GroupingExpression expr);
    }

    /// <summary>
    /// Base class for all types of expressions.
    /// </summary>
    public abstract class Expression
    {
        public abstract T Accept<T>(IVisitor<T> visitor);
    }

    /// <summary>
    /// Represents a binary expression composed of a left expression, an operator token, and a right expression.
    /// </summary>
    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(Expression left, Token op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
    }

    /// <summary>
    /// Represents a unary expression composed of an operator token and a right expression.
    /// </summary>
    public class UnaryExpression : Expression
    {
        public Token Operator { get; }
        public Expression Right { get; }

        public UnaryExpression(Token op, Expression right)
        {
            Operator = op;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }
    }

    /// <summary>
    /// Represents a literal value expression, such as a number, boolean or string.
    /// </summary>
    public class LiteralExpression : Expression
    {
        public object Value { get; }

        public LiteralExpression(object value)
        {
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLiteralExpression(this);
        }
    }

    /// <summary>
    /// Represents a grouping expression that wraps another expression.
    /// </summary>
    public class GroupingExpression : Expression
    {
        public Expression Expression { get; }

        public GroupingExpression(Expression expression)
        {
            Expression = expression;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitGroupingExpression(this);
        }
    }
}
