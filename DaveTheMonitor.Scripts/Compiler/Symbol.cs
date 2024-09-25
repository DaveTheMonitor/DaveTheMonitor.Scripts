using DaveTheMonitor.Scripts.Compiler.Nodes;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class Symbol
    {
        public string Identifier { get; set; }
        private List<SymbolUsage> _usages;

        public void AddUsage(ExpressionNode node, ScriptType type)
        {
            _usages.Add(new SymbolUsage(node, type));
        }

        public SymbolUsage? GetUsage(ExpressionNode node)
        {
            foreach (SymbolUsage usage in _usages)
            {
                if (usage.Node == node)
                {
                    return usage;
                }
            }
            return null;
        }

        public Symbol(string identifier)
        {
            Identifier = identifier;
            _usages = new List<SymbolUsage>();
        }
    }
}
