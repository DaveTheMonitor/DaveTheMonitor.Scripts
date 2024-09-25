using System;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal abstract class ExpressionNode : ICloneable
    {
        public ScriptToken Start { get; private set; }
        public ScriptType ResultType { get; set; }

        public static ExpressionNode[] Clone(ExpressionNode[] array)
        {
            ExpressionNode[] nodes = new ExpressionNode[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                nodes[i] = array[i].Clone();
            }
            return nodes;
        }

        public abstract ExpressionNode Clone();

        protected string GetResultString()
        {
            return ResultType != null ? $"<{ResultType.Name}>" : "";
        }

        object ICloneable.Clone() => Clone();

        public ExpressionNode(ScriptToken start)
        {
            Start = start;
        }
    }
}
