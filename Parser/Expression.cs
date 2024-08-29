using System;
using System.Collections.Generic;

namespace GwentInterpreters
{
    public abstract class Expression
    {
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
            T VisitGetExpression(Get expr);
            T VisitSetExpression(Set expr);
            T VisitEffectInvocationExpr(EffectInvocation expr);
            T VisitSelectorExpr(Selector expr);
            T VisitActionExpression(Action expr);
            T VisitPredicate(Predicate expr);
            T VisitEffectAction(EffectAction expr);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);
        public abstract override string ToString();
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

        public override string ToString()
        {
            return $"{Name.Lexeme} = {Value}";
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

        public override string ToString()
        {
            return $"({Left} {Operator.Lexeme} {Right})";
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

        public override string ToString()
        {
            return $"({Operator.Lexeme} {Right})";
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

        public override string ToString()
        {
            return Value.ToString();
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

        public override string ToString()
        {
            return $"({Expression})";
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

        public override string ToString()
        {
            return name.Lexeme;
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

        public override string ToString()
        {
            return $"({Left} {Operator.Lexeme} {Right})";
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

        public override string ToString()
        {
            return $"({Left}{Operator.Lexeme})";
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

        public override string ToString()
        {
            return $"{Callee}({string.Join(", ", Arguments)})";
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

        public override string ToString()
        {
            return $"{Object}.{Name.Lexeme}";
        }
    }

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

        public override string ToString()
        {
            return $"{Object}.{Name.Lexeme} = {Value}";
        }
    }

    public class Action : Expression
    {
        public Token TargetParam { get; }
        public Token ContextParam { get; }
        public List<Stmt> Body { get; }

        public Action(Token targetParam, Token contextParam, List<Stmt> body)
        {
            TargetParam = targetParam;
            ContextParam = contextParam;
            Body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitActionExpression(this);
        }

        public override string ToString()
        {
            return $"({TargetParam.Lexeme}, {ContextParam.Lexeme}) => {{ {string.Join("; ", Body)} }}";
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

        public override string ToString()
        {
            return $"Effect: {Name}({string.Join(", ", Parameters)})";
        }
    }

    public class Selector : Expression
    {
        public string Source { get; }
        public bool Single { get; }
        public Predicate Predicate { get; }

        public Selector(string source, bool single, Predicate predicate)
        {
            Source = source;
            Single = single;
            Predicate = predicate;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSelectorExpr(this);
        }

        public override string ToString()
        {
            return $"Selector: {{ Source: {Source}, Single: {Single}, Predicate: {Predicate} }}";
        }
    }

    public class Predicate : Expression
    {
        public Token Parameter { get; }
        public Expression Body { get; }

        public Predicate(Token parameter, Expression body)
        {
            Parameter = parameter;
            Body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPredicate(this);
        }

        public override string ToString()
        {
            return $"({Parameter.Lexeme}) => {Body}";
        }
    }

    public class EffectAction : Expression
    {
        public EffectInvocation Effect { get; }
        public Selector Selector { get; }
        public EffectAction PostAction { get; }

        public EffectAction(EffectInvocation effect, Selector selector, EffectAction postAction)
        {
            Effect = effect;
            Selector = selector;
            PostAction = postAction;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitEffectAction(this);
        }

        public override string ToString()
        {
            return $"EffectAction: {{ Effect: {Effect}, Selector: {Selector}, PostAction: {PostAction} }}";
        }
    }
}