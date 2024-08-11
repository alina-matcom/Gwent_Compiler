using System;

namespace GwentInterpreters
{
    /// <summary>
    /// Base class for all types of expressions.
    /// </summary>
    public abstract class Expression
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
            T VisitVariableExpr(Variable expr);
            T VisitAssignExpression(AssignExpression expr);
            T VisitLogicalExpression(LogicalExpression expr); // Método añadido para LogicalExpression
        }

        public abstract T Accept<T>(IVisitor<T> visitor);
    }

    public class AssignExpression : Expression
    {
        public Token Name { get; }
        public Expression Value { get; }

        public AssignExpression(Token name, Expression value)
        {
            Name = name;
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAssignExpression(this);
        }
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

    public class Variable : Expression
    {
        public readonly Token name;
        public Variable(Token name)
        {
            this.name = name;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVariableExpr(this);
        }
    }

    /// <summary>
    /// Represents a logical expression composed of a left expression, an operator token, and a right expression.
    /// </summary>
    public class LogicalExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public LogicalExpression(Expression left, Token op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLogicalExpression(this);
        }
    }
}
