using System;
using System.IO;
using System.Collections.Generic;

namespace GwentInterpreters
{
    class Program
    {
        static void Main(string[] args)
        {
            // Leer el archivo de prueba
            string dslCode = File.ReadAllText(@"C:\Users\admin\Desktop\Proyecto Gwent++\test.dsl");

            // Parsear el código DSL
            Parser parser = new Parser(new Scanner(dslCode).ScanTokens());
            List<Stmt> statements = parser.Parse();

            // Crear una instancia del intérprete
            Interpreter interpreter = new Interpreter();

            // Interpretar las declaraciones parseadas
            List<Card> cards = interpreter.Interpret(statements);

            // Imprimir los resultados del parseo
            foreach (var stmt in statements)
            {
                Console.WriteLine(stmt);
            }

            // Imprimir los resultados del intérprete
            foreach (var card in cards)
            {
                Console.WriteLine(card);
            }
        }
    }
}