using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class WhileStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode[] Body { get; private set; }

        public override WhileStatementNode Clone()
        {
            return new WhileStatementNode(Start, Condition.Clone(), Clone(Body));
        }

        public override string ToString()
        {
            return $"While {Condition} Do\n{GetBodyString()}\nEnd";
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

        public WhileStatementNode(ScriptToken start, ExpressionNode condition, StatementNode[] body) : base(start)
        {
            Condition = condition;
            Body = body;
        }
    }
}
