namespace GwentInterpreters
{
    /// <summary>
    /// Represents a token in the source code.
    /// </summary>
   public class Token
    {
        public TokenType Type { get; }  // The type of the token
        public string Lexeme { get; }   // The lexeme of the token
        public object Literal { get; }  // The literal value of the token
        public CodeLocation Location { get; private set; } // The location of the token in the source code

        // Modificado para incluir la información de Location
        public Token(TokenType type, string lexeme, Object literal, CodeLocation location)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            Location = location;
        }

        public override string ToString()
        {
            return Type + " " + Lexeme + " " + (Literal?? "null");
        }
    }

    // Definición del struct CodeLocation
    public struct CodeLocation
    {
        public string File;
        public int Line;
        public int Column;
    }
}