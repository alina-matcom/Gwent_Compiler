using System;
using System.Collections.Generic;

namespace GwentInterpreters
{
    public class EffectDefinition
    {
        // Referencia a la declaración de efecto.
        private readonly EffectStmt _declaration;

        // Constructor para inicializar el efecto.
        public EffectDefinition(EffectStmt declaration)
        {
            _declaration = declaration;
        }
    }
}
