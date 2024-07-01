namespace GwentInterpreters
{
    public enum TokenType
    {
        // Palabras clave específicas del DSL
        EFFECT, // Representa la palabra clave "effect"
        CARD,   // Representa la palabra clave "card"
        NAME,   // Representa la palabra clave "name"
        TYPE,   // Representa la palabra clave "type"
        FACTION,// Representa la palabra clave "faction"
        POWER,  // Representa la palabra clave "power"
        RANGE,  // Representa la palabra clave "range"
        ACTION, // Representa la palabra clave "action"
        PARAMS, // Representa la palabra clave "params"

        ONACTIVATION, // Representa la palabra clave "onActivation"
        SELECTOR,     // Representa la palabra clave "selector"
        PREDICATE,    // Representa la palabra clave "predicate"
        POSTACTION,   // Representa la palabra clave "postAction"

        // Nuevos tokens para declaraciones
        CONST, // Representa la palabra clave "const"
        VAR,   // Representa la palabra clave "var"
        TEMP,  // Representa la palabra clave "temp"

        // Valores literales y tipos de cartas
        IDENTIFIER,     // Representa un identificador (nombre de variable)
        STRING,         // Representa un valor de cadena
        NUMBER,         // Representa un valor numérico
        BOOLEAN,        // Representa un valor booleano
        GOLD,           // Representa un tipo de carta "gold"
        SILVER,         // Representa un tipo de carta "silver"
        WEATHER,        // Representa un tipo de carta "weather"
        BOOST,          // Representa un tipo de carta "boost"
        LEADER,         // Representa un tipo de carta "leader"
        MELEE,          // Representa un tipo de carta "melee"
        RANGED,         // Representa un tipo de carta "ranged"
        SIEGE,          // Representa un tipo de carta "siege"

        // Símbolos y operadores
        LEFT_BRACE,     // Representa el símbolo "{" (llave izquierda)
        RIGHT_BRACE,    // Representa el símbolo "}" (llave derecha)
        LEFT_PAREN,     // Representa el símbolo "(" (paréntesis izquierdo)
        RIGHT_PAREN,    // Representa el símbolo ")" (paréntesis derecho)
        LEFT_BRACKET,   // Representa el símbolo "[" (corchete izquierdo)
        RIGHT_BRACKET,  // Representa el símbolo "]" (corchete derecho)
        COMMA,          // Representa el símbolo ","
        DOT,            // Representa el símbolo "."
        PLUS,           // Representa el operador "+"
        MINUS,          // Representa el operador "-"
        STAR,           // Representa el operador "*"
        SLASH,          // Representa el operador "/"
        ASSIGN,         // Representa el operador de asignación "="
        SEMICOLON,      // Representa un punto y coma ";"
        NOT_EQUAL,      // Represents the inequality operator "!="
        EQUAL_EQUAL,    // Representa el operador "=="
        GREATER,        // Representa el operador ">"
        GREATER_EQUAL,  // Representa el operador ">="
        LESS,           // Representa el operador "<"
        LESS_EQUAL,     // Representa el operador "<="
        CONCAT,         // Representa el operador "@" (concatenación)
        INCREMENT,      // Representa el operador "++" (incremento)
        DECREMENT,      // Representa el operador "--" (decremento)
        LAMBDA,         // Representa el operador de función lambda "=>"

        // Operadores lógicos
        AND, // Representa el operador lógico "and"
        OR,  // Representa el operador lógico "or"
        NOT, // Representa el operador lógico "not"

        // Estructuras de control
        FOR, // Representa la estructura de control "for"
        WHILE, // Representa la estructura de control "while"
        IF,   // Representa la estructura de control "if"
        ELSE, // Representa la estructura de control "else"

        // Métodos y propiedades del contexto del juego
        TRIGGERPLAYER, // Representa el método o propiedad "triggerPlayer"
        BOARD,         // Representa el método o propiedad "board"
        HANDOFPLAYER,  // Representa el método o propiedad "handOfPlayer"
        FIELD_OF_PLAYER,// Representa el método o propiedad "fieldOfPlayer"
        GRAVEYARD_OF_PLAYER, // Representa el método o propiedad "graveyardOfPlayer"
        DECK_OF_PLAYER,   // Representa el método o propiedad "deckOfPlayer"
        OWNER,           // Representa el método o propiedad "owner"
        FIND,            // Representa el método o propiedad "find"
        PUSH,            // Representa el método o propiedad "push"
        SENDBOTTOM,      // Representa el método o propiedad "sendBottom"
        POP,             // Representa el método o propiedad "pop"
        REMOVE,          // Representa el método o propiedad "remove"
        SHUFFLE,         // Representa el método o propiedad "shuffle"

        // Token para el fin de archivo
        EOF // Representa el marcador del final del archivo
    }
}