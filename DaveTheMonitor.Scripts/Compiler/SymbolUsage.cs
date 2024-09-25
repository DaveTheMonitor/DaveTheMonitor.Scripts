using DaveTheMonitor.Scripts.Compiler.Nodes;

namespace DaveTheMonitor.Scripts.Compiler
{
    // Variables in CSRScript are dynamic so their type can change,
    // but the semantic analyzer can still keep track of the variable's
    // type for specific usages to allow certain compiler optimizations.
    internal struct SymbolUsage
    {
        public ExpressionNode Node { get; set; }
        public ScriptType Type { get; set; }

        public SymbolUsage(ExpressionNode node, ScriptType type)
        {
            Node = node;
            Type = type;
        }
    }
}
