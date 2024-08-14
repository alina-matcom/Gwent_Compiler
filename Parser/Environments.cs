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

        // Asignar un valor a una variable existente o crearla implícitamente si no existe
        public void Assign(Token name, object value)
        {
            // Verificar si la variable existe en el entorno actual
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = value;
                return;
            }

            // Verificar si la variable existe en un entorno superior
            if (enclosing != null)
            {
                enclosing.Assign(name, value);
                return;
            }

            // Si la variable no existe en ningún entorno, se define en el entorno actual
            Define(name.Lexeme, null);
            values[name.Lexeme] = value;
        }
    }
}
