namespace GwentInterpreters
{
    public class Environment
{
    private readonly Environment enclosing;
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();

    // Constructor para el entorno global
    public Environment()
    {
        enclosing = null;
    }

    // Constructor para un entorno local anidado dentro de otro
    public Environment(Environment enclosing)
    {
        this.enclosing = enclosing;
    }

    // Definir una nueva variable en el entorno actual
    public void Define(string name, object value)
    {
        values[name] = value;
    }

    // Obtener el valor de una variable
    public object Get(Token name)
    {
        if (values.TryGetValue(name.Lexeme, out var value))
        {
            return value;
        }

        if (enclosing != null)
        {
            return enclosing.Get(name);
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    // Asignar un valor a una variable existente
    public void Assign(Token name, object value)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            values[name.Lexeme] = value;
            return;
        }

        if (enclosing != null)
        {
            enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}
}