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
            void VisitActionStmt(ActionStmt stmt);
            void VisitCardStmt(CardStmt stmt);
            void VisitOnActivationStmt(OnActivationStmt stmt);
            void VisitSelectorStmt(SelectorStmt stmt);
            void VisitPostActionStmt(PostActionStmt stmt);
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
        public Expression Iterable { get; }
        public List<Stmt> Body { get; }

        public For(Token iterator, Expression iterable, List<Stmt> body)
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
        public Dictionary<string, Type> Params { get; }
        public ActionStmt Action { get; }

        public EffectStmt(string name, Dictionary<string, Type> @params, ActionStmt action)
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

    public class ActionStmt : Stmt
    {
        public Expression Targets { get; }
        public List<Stmt> Body { get; }

        public ActionStmt(Expression targets, List<Stmt> body)
        {
            Targets = targets;
            Body = body;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitActionStmt(this);
        }
    }

    public class CardStmt : Stmt
    {
        public string Type { get; }
        public string Name { get; }
        public string Faction { get; }
        public int Power { get; }
        public List<string> Range { get; }
        public List<OnActivationStmt> OnActivation { get; }

        public CardStmt(string type, string name, string faction, int power, List<string> range, List<OnActivationStmt> onActivation)
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

    public class OnActivationStmt : Stmt
    {
        public EffectStmt Effect { get; }
        public SelectorStmt Selector { get; }
        public PostActionStmt PostAction { get; }

        public OnActivationStmt(EffectStmt effect, SelectorStmt selector, PostActionStmt postAction)
        {
            Effect = effect;
            Selector = selector;
            PostAction = postAction;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitOnActivationStmt(this);
        }
    }

    public class SelectorStmt : Stmt
    {
        public string Source { get; }
        public bool Single { get; }
        public Expression Predicate { get; }

        public SelectorStmt(string source, bool single, Expression predicate)
        {
            Source = source;
            Single = single;
            Predicate = predicate;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitSelectorStmt(this);
        }
    }

    public class PostActionStmt : Stmt
    {
        public EffectStmt Effect { get; }
        public SelectorStmt Selector { get; }

        public PostActionStmt(EffectStmt effect, SelectorStmt selector)
        {
            Effect = effect;
            Selector = selector;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitPostActionStmt(this);
        }
    }
}
