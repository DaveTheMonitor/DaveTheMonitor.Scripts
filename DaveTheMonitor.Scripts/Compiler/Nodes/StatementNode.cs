using System;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal abstract class StatementNode : ICloneable
    {
        public ScriptToken Start { get; private set; }
        public abstract StatementNode Clone();

        public static StatementNode[] Clone(StatementNode[] array)
        {
            StatementNode[] nodes = new StatementNode[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                nodes[i] = array[i].Clone();
            }
            return nodes;
        }

        object ICloneable.Clone() => Clone();

        public StatementNode(ScriptToken start)
        {
            Start = start;
        }
    }
}
