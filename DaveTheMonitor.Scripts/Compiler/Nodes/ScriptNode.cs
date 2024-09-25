using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ScriptNode
    {
        public StatementNode[] Children { get; private set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (StatementNode node in Children)
            {
                builder.AppendLine(node.ToString());
            }
            return builder.ToString();
        }

        public ScriptNode Clone()
        {
            StatementNode[] nodes = new StatementNode[Children.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = Children[i].Clone();
            }
            return new ScriptNode(nodes);
        }

        public ScriptNode(StatementNode[] nodes)
        {
            Children = nodes;
        }
    }
}
