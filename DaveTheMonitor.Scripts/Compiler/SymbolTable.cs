using DaveTheMonitor.Scripts.Compiler.Nodes;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class SymbolTable
    {
        private Dictionary<string, Symbol> _symbols;

        public bool HasSymbol(string identifier)
        {
            return _symbols.ContainsKey(identifier);
        }

        public bool TryGetValue(string identifier, out Symbol symbol)
        {
            return _symbols.TryGetValue(identifier, out symbol);
        }

        public Symbol GetSymbol(string identifier)
        {
            return _symbols[identifier];
        }

        public Symbol[] GetAllSymbols()
        {
            Symbol[] symbols = new Symbol[_symbols.Count];
            int i = 0;
            foreach (KeyValuePair<string, Symbol> pair in _symbols)
            {
                symbols[i] = pair.Value;
                i++;
            }
            return symbols;
        }

        public Symbol AddSymbol(string identifier)
        {
            Symbol symbol = new Symbol(identifier);
            AddSymbol(symbol);
            return symbol;
        }

        public void AddSymbol(Symbol symbol)
        {
            _symbols.Add(symbol.Identifier, symbol);
        }

        public void AddSymbolUsage(string identifier, ExpressionNode node, ScriptType type)
        {
            GetSymbol(identifier).AddUsage(node, type);
        }

        public void Clear()
        {
            _symbols.Clear();
        }

        public SymbolTable()
        {
            _symbols = new Dictionary<string, Symbol>();
        }
    }
}
