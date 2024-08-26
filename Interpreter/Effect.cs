using System;
using System.Collections.Generic;

namespace GwentInterpreters
{
    public class EffectDefinition
    {
        // Lista de parámetros del efecto.
        public List<Parameter> Parameters { get; }

        // Instancia de la función de acción asociada con este efecto.
        public ActionFunction ActionFunction { get; }

        // Constructor para inicializar el efecto.
        public EffectDefinition(List<Parameter> parameters, ActionFunction actionFunction)
        {
            Parameters = parameters;
            ActionFunction = actionFunction;
        }
    }


    public class ActionFunction
    {
        private readonly Action declaration;

        public ActionFunction(Action declaration)
        {
            this.declaration = declaration;
        }

        public void Call(Interpreter interpreter, Iterable targets, Context context, Dictionary<string, object> arguments)
        {
            // Crear un nuevo entorno para la ejecución del bloque de la acción
            Environment environment = new Environment(interpreter.environment);

            // Definir en el entorno las variables provenientes del diccionario de argumentos
            foreach (var entry in arguments)
            {
                environment.Define(entry.Key, entry.Value);
            }

            // Definir los `targets` y `context` en el entorno usando los nombres de los tokens
            environment.Define(declaration.TargetParam.Lexeme, targets);
            environment.Define(declaration.ContextParam.Lexeme, context);

            // Ejecutar el bloque de código con el nuevo entorno
            interpreter.ExecuteBlock(declaration.Body, environment);

            // No se devuelve ningún valor porque es una función de tipo void
        }
    }
}
