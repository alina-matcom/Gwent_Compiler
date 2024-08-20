using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GwentInterpreters
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ruta del archivo de texto de GWENT++
            string filePath = @"C:\Users\Alina\Desktop\Txt\gwent_deck.txt";

            try
            {
                // Lee todo el contenido del archivo a una cadena
                string inputText = File.ReadAllText(filePath, Encoding.UTF8);

                // Crea una instancia del Scanner con el contenido del archivo
                Scanner scanner = new Scanner(inputText);

                // Obtiene la lista de tokens
                List<Token> tokens = scanner.ScanTokens();

                // Imprime los tokens
                Console.WriteLine("Tokens generados:");
                foreach (Token token in tokens)
                {
                    Console.WriteLine(token.ToString());
                }

                // Crea una instancia del Parser con la lista de tokens
                Parser parser = new Parser(tokens);

                // Obtiene la lista de declaraciones (statements)
                List<Stmt> statements = parser.Parse();

                // Imprime los resultados del parser
                Console.WriteLine("Declaraciones parseadas:");
                foreach (Stmt statement in statements)
                {
                    Console.WriteLine(statement.ToString());
                }

                // Crea una instancia del Interpreter con las declaraciones
                Interpreter interpreter = new Interpreter();

                // Interpreta las declaraciones
                interpreter.Interpret(statements);

                // Imprime un mensaje indicando que la interpretación se ha completado
                Console.WriteLine("Interpretación completada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar el archivo: {ex.Message}");
            }
        }
    }
}