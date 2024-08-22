namespace GwentInterpreters
{
    public abstract class Stmt
    {
        public interface IVisitor
        {
            void VisitExprStmt(Expr stmt);
            void VisitBlockStmt(Block stmt);
            void VisitIfStmt(If stmt);
            void VisitWhileStmt(While stmt);
            void VisitForStmt(For stmt); // Añadido para la clase For

            void VisitEffectStmt(EffectStmt stmt);
            void VisitActionStmt(Action stmt);
            void VisitCardStmt(CardStmt stmt);
            void VisitEffectAction(EffectAction stmt);
        }

        public abstract void Accept(IVisitor visitor);
    }

    public class Expr : Stmt
    {
        public readonly Expression expression;

        public Expr(Expression expression)
        {
            this.expression = expression;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitExprStmt(this);
        }
    }

    public class Block : Stmt
    {
        public readonly List<Stmt> statements;

        public Block(List<Stmt> statements)
        {
            this.statements = statements;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitBlockStmt(this);
        }
    }

    public class If : Stmt
    {
        public readonly Expression condition;
        public readonly Stmt thenBranch;
        public readonly Stmt elseBranch;

        public If(Expression condition, Stmt thenBranch, Stmt elseBranch)
        {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitIfStmt(this);
        }
    }

    public class While : Stmt
    {
        public Expression Condition { get; }
        public Stmt Body { get; }

        public While(Expression condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitWhileStmt(this);
        }
    }

    public class For : Stmt
    {
        public Token Iterator { get; }
        public Token Iterable { get; }
        public List<Stmt> Body { get; }

        public For(Token iterator, Token iterable, List<Stmt> body)
        {
            Iterator = iterator;
            Iterable = iterable;
            Body = body;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitForStmt(this);
        }
    }

    public class EffectStmt : Stmt
    {
        public string Name { get; }
        public List<Parameter> Params { get; }
        public Action Action { get; }

        public EffectStmt(string name, List<Parameter> @params, Action action)
        {
            Name = name;
            Params = @params;
            Action = action;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitEffectStmt(this);
        }
    }

    public class Parameter
    {
        public Token Name { get; }
        public Token Type { get; }

        public Parameter(Token name, Token type)
        {
            Name = name;
            Type = type;
        }
    }
    public class Action : Stmt
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

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitActionStmt(this);
        }
    }

    public class EffectAction : Stmt
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

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitEffectAction(this);
        }
    }

    public class CardStmt : Stmt
    {
        public string Type { get; }
        public string Name { get; }
        public string Faction { get; }
        public Expression Power { get; }
        public List<string> Range { get; }
        public List<EffectAction> OnActivation { get; }

        public CardStmt(string type, string name, string faction, Expression power, List<string> range, List<EffectAction> onActivation)
        {
            Type = type;
            Name = name;
            Faction = faction;
            Power = power;
            Range = range;
            OnActivation = onActivation;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitCardStmt(this);
        }
    }
}
