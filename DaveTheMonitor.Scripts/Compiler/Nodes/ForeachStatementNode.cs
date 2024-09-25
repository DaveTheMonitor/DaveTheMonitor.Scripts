using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ForeachStatementNode : StatementNode
    {
        public ScriptToken ItemIdentifier { get; private set; }
        public ExpressionNode Iterator { get; private set; }
        public StatementNode[] Body { get; private set; }

        public override ForeachStatementNode Clone()
        {
            return new ForeachStatementNode(Start, ItemIdentifier, Iterator.Clone(), Clone(Body));
        }

        public override string ToString()
        {
            return $"Foreach\nVar [{ItemIdentifier.Lexeme}] In {Iterator}\nDo\n{GetBodyString()}\nEnd";
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

        public ForeachStatementNode(ScriptToken start, ScriptToken itemIdentifier, ExpressionNode iterator, StatementNode[] body) : base(start)
        {
            ItemIdentifier = itemIdentifier;
            Iterator = iterator;
            Body = body;
        }
    }
}
