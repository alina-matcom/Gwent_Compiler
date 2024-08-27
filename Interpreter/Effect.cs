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


    public class EffectInstance
    {
        // El nombre del efecto.
        public string Name { get; }

        // Diccionario que guarda los argumentos del efecto.
        public Dictionary<string, object> Arguments { get; }

        // La función de acción asociada a esta instancia de efecto.
        public ActionFunction ActionFunction { get; }

        // Constructor para inicializar una instancia de efecto.
        public EffectInstance(string name, Dictionary<string, object> arguments, ActionFunction actionFunction)
        {
            Name = name;
            Arguments = arguments;
            ActionFunction = actionFunction;
        }

        // Método para ejecutar la acción de este efecto.
        public void Invoke(Interpreter interpreter, Iterable targets, Context context)
        {
            ActionFunction.Call(interpreter, targets, context, Arguments);
        }
    }

    public class SelectorResult
    {
        public string Source { get; }
        public bool Single { get; }
        public Func<Card, bool> Predicate { get; }

        public SelectorResult(string source, bool single, Func<Card, bool> predicate)
        {
            Source = source;
            Single = single;
            Predicate = predicate;
        }
    }

    public class EffectActionResult
    {
        // Instancia del efecto.
        public EffectInstance EffectInstance { get; }

        // Resultado del selector.
        public SelectorResult SelectorResult { get; }

        // Resultado del post-action.
        public EffectActionResult PostActionResult { get; }

        // Constructor para inicializar los campos.
        public EffectActionResult(EffectInstance effectInstance, SelectorResult selectorResult, EffectActionResult postActionResult)
        {
            EffectInstance = effectInstance;
            SelectorResult = selectorResult;
            PostActionResult = postActionResult;
        }
    }


}
