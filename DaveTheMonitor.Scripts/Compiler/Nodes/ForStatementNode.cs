using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ForStatementNode : StatementNode
    {
        public StatementNode Initializer { get; private set; }
        public ExpressionNode Condition { get; private set; }
        public StatementNode Iterator { get; private set; }
        public StatementNode[] Body { get; private set; }

        public override ForStatementNode Clone()
        {
            return new ForStatementNode(Start, Initializer.Clone(), Condition.Clone(), Iterator.Clone(), Clone(Body));
        }

        public override string ToString()
        {
            return $"For\n{Initializer}\n{Condition}\n{Iterator}\nDo\n{GetBodyString()}\nEnd";
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

        public ForStatementNode(ScriptToken start, StatementNode initializer, ExpressionNode condition, StatementNode iterator, StatementNode[] body) : base(start)
        {
            Initializer = initializer;
            Condition = condition;
            Iterator = iterator;
            Body = body;
        }
    }
}
