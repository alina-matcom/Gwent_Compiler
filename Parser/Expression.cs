using System;
using System.Collections.Generic;

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
            T VisitLogicalExpression(LogicalExpression expr);
            T VisitPostfixExpression(PostfixExpression expr);
            T VisitCallExpression(Call expr);
            T VisitGetExpression(Get expr); // Método añadido para GetExpression
            T VisitSetExpression(Set expr); // Método añadido para SetExpression
            T VisitEffectInvocationExpr(EffectInvocation expr);
            T VisitSelectorExpr(Selector expr);
            T VisitLambdaExpression(LambdaExpression lambda);
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

    public class PostfixExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }

        public PostfixExpression(Expression left, Token operatorToken)
        {
            Left = left;
            Operator = operatorToken;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPostfixExpression(this);
        }
    }

    public class Call : Expression
    {
        public Expression Callee { get; }
        public Token Paren { get; }
        public List<Expression> Arguments { get; }

        public Call(Expression callee, Token paren, List<Expression> arguments)
        {
            Callee = callee;
            Paren = paren;
            Arguments = arguments;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitCallExpression(this);
        }
    }

    public class Get : Expression
    {
        public Expression Object { get; }
        public Token Name { get; }

        public Get(Expression obj, Token name)
        {
            Object = obj;
            Name = name;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitGetExpression(this);
        }
    }

    // Nueva clase para las expresiones de asignación (setter)
    public class Set : Expression
    {
        public Expression Object { get; }
        public Token Name { get; }
        public Expression Value { get; }

        public Set(Expression obj, Token name, Expression value)
        {
            Object = obj;
            Name = name;
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSetExpression(this);
        }
    }

    public class EffectInvocation : Expression
    {
        public string Name { get; }
        public Dictionary<string, Expression> Parameters { get; }

        public EffectInvocation(string name, Dictionary<string, Expression> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitEffectInvocationExpr(this);
        }
    }

    /// <summary>
    /// Nodo que representa un selector.
    /// </summary>
    public class Selector : Expression
    {
        public string Source { get; }
        public bool Single { get; }
        public Expression Predicate { get; }

        public Selector(string source, bool single, Expression predicate)
        {
            Source = source;
            Single = single;
            Predicate = predicate;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSelectorExpr(this);
        }
    }

    public class LambdaExpression : Expression
    {
        public Token Parameter { get; }
        public Expression Body { get; }

        public LambdaExpression(Token parameter, Expression body)
        {
            Parameter = parameter;
            Body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLambdaExpression(this);
        }
    }

}
