using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode[] Body { get; private set; }
        public StatementNode[] Else { get; private set; }

        public override IfStatementNode Clone()
        {
            return new IfStatementNode(Start, Condition.Clone(), Clone(Body), Clone(Else));
        }

        public override string ToString()
        {
            return $"If {Condition} Then\n{GetBodyString()}{GetElseString()}";
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

        private string GetElseString()
        {
            if (Else == null)
            {
                return "EndIf";
            }

            StringBuilder builder = new StringBuilder("Else\n");
            foreach (StatementNode node in Else)
            {
                builder.AppendLine(node.ToString());
            }

            builder.AppendLine("EndIf");
            return builder.ToString();
        }

        public IfStatementNode(ScriptToken start, ExpressionNode condition, StatementNode[] body, StatementNode[] elseStatements) : base(start)
        {
            Condition = condition;
            Body = body;
            Else = elseStatements;
        }
    }
}
