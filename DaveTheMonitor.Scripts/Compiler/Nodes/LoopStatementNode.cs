using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class LoopStatementNode : StatementNode
    {
        public ExpressionNode Count { get; private set; }
        public StatementNode[] Body { get; private set; }
        public bool LiteralCount { get; set; }

        public override LoopStatementNode Clone()
        {
            return new LoopStatementNode(Start, Count.Clone(), Clone(Body));
        }

        public override string ToString()
        {
            return $"Loop {Count}\n{GetBodyString()}\nEnd";
        }

        private string GetBodyString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (StatementNode node in Body)
            {
                builder.AppendLine(node.ToString());
            }
            return builder.ToString();
        }

        public LoopStatementNode(ScriptToken start, ExpressionNode count, StatementNode[] body) : base(start)
        {
            Count = count;
            Body = body;
        }
    }
}
