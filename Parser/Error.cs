using System;

namespace GwentInterpreters
{
    /// <summary>
    /// Represents an error that occurs during the execution of the interpreter.
    /// </summary>
    public class Error : Exception
    {
        public ErrorType ErrorType { get; private set; }    // El tipo de error

        /// <summary>
        /// Constructor de la clase Error.
        /// </summary>
        /// <param name="errorType">El tipo de error (ErrorType).</param>
        /// <param name="message">El mensaje asociado al error.</param>
        public Error(ErrorType errorType, string message) : base(message)
        {
            ErrorType = errorType;
        
        }

        /// <summary>
        /// Retorna una representación en cadena del error.
        /// </summary>
        /// <returns>Una cadena que describe el tipo y mensaje de error.</returns>
        public override string ToString()
        {
            return $"! {ErrorType} ERROR: {base.Message}";
        }
    }

    /// <summary>
    /// Enumerates the types of errors that can occur during the execution of the interpreter.
    /// </summary>
    public enum ErrorType
    {
        LEXICAL,    // Lexical errors occur when the lexer encounters an invalid character
        SYNTAX,     // Syntax errors occur when the parser encounters an invalid token
        SEMANTIC    // Semantic errors occur when the evaluator encounters an invalid expression
    }
}
