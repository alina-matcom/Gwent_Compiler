using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GwentInterpreters
{
    public class Card
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Faction { get; set; }
        public int Power { get; set; }
        public List<string> Range { get; set; }

        public Card(string type, string name, string faction, int power, List<string> range)
        {
            Type = type;
            Name = name;
            Faction = faction;
            Power = power;
            Range = range;
        }
    }

    public class Context
    {
        private int triggerPlayer;
        private List<Card> board;
        private Dictionary<int, Iterable> hands;
        private Dictionary<int, Iterable> fields;
        private Dictionary<int, Iterable> graveyards;
        private Dictionary<int, Iterable> decks;

        public Context(int triggerPlayer)
        {
            this.triggerPlayer = triggerPlayer;
            board = new List<Card>();
            hands = new Dictionary<int, Iterable>();
            fields = new Dictionary<int, Iterable>();
            graveyards = new Dictionary<int, Iterable>();
            decks = new Dictionary<int, Iterable>();
        }

        public int TriggerPlayer => triggerPlayer;
        public List<Card> Board => board;

        public Iterable HandOfPlayer(int player) => hands.ContainsKey(player) ? hands[player] : new Iterable();
        public Iterable FieldOfPlayer(int player) => fields.ContainsKey(player) ? fields[player] : new Iterable();
        public Iterable GraveyardOfPlayer(int player) => graveyards.ContainsKey(player) ? graveyards[player] : new Iterable();
        public Iterable DeckOfPlayer(int player) => decks.ContainsKey(player) ? decks[player] : new Iterable();

        public Iterable Hand => HandOfPlayer(triggerPlayer);
        public Iterable Field => FieldOfPlayer(triggerPlayer);
        public Iterable Graveyard => GraveyardOfPlayer(triggerPlayer);
        public Iterable Deck => DeckOfPlayer(triggerPlayer);
    }

    public class Iterable : IList<Card>
    {
        private List<Card> cards;

        public Iterable()
        {
            cards = new List<Card>();
        }

        // Implementación de IList<Card>
        public Card this[int index] { get => cards[index]; set => cards[index] = value; }
        public int Count => cards.Count;
        public bool IsReadOnly => false;

        public void Add(Card card) => cards.Add(card);
        public void Clear() => cards.Clear();
        public bool Contains(Card card) => cards.Contains(card);
        public void CopyTo(Card[] array, int arrayIndex) => cards.CopyTo(array, arrayIndex);
        public IEnumerator<Card> GetEnumerator() => cards.GetEnumerator();
        public int IndexOf(Card card) => cards.IndexOf(card);
        public void Insert(int index, Card card) => cards.Insert(index, card);
        public bool Remove(Card card) => cards.Remove(card);
        public void RemoveAt(int index) => cards.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => cards.GetEnumerator();

        // Métodos adicionales
        public List<Card> Find(Func<Card, bool> predicate) => cards.Where(predicate).ToList();

        public void Push(Card card) => cards.Add(card);

        public void SendBottom(Card card) => cards.Insert(0, card);

        public Card Pop()
        {
            if (cards.Count == 0)
                throw new InvalidOperationException("No hay cartas en la colección.");

            Card card = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            return card;
        }

        public void Shuffle()
        {
            Random rng = new Random();
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
        }
    }

    public class CallableMethod
    {
        private readonly object _instance;
        private readonly MethodInfo _method;

        public CallableMethod(object instance, MethodInfo method)
        {
            _instance = instance;
            _method = method;
        }

        public bool CanInvoke(List<object> arguments, out string errorMessage)
        {
            var parameters = _method.GetParameters();
            if (parameters.Length != arguments.Count)
            {
                errorMessage = $"Se esperaban {parameters.Length} argumentos, pero se obtuvieron {arguments.Count}.";
                return false;
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var paramType = parameters[i].ParameterType;
                if (arg != null && !paramType.IsAssignableFrom(arg.GetType()))
                {
                    errorMessage = $"El argumento {i + 1} no puede ser convertido al tipo {paramType.Name}.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        public object Call(List<object> arguments)
        {
            var parameters = _method.GetParameters();
            var convertedArgs = new object[arguments.Count];
            for (int i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var paramType = parameters[i].ParameterType;
                convertedArgs[i] = Convert.ChangeType(arg, paramType);
            }

            return _method.Invoke(_instance, convertedArgs);
        }
    }


}
