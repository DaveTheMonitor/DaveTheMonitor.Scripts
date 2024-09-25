using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class UsingStatementNode : StatementNode
    {
        public ScriptToken[] Identifiers { get; private set; }

        public override UsingStatementNode Clone()
        {
            return new UsingStatementNode(Start, ScriptToken.Clone(Identifiers));
        }

        public override string ToString()
        {
            return $"Using {GetIdentifiersString()}";
        }

        private string GetIdentifiersString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Identifiers.Length; i++)
            {
                builder.Append('[');
                builder.Append(Identifiers[i].Lexeme);
                if (i < Identifiers.Length - 1)
                {
                    builder.Append("] ");
                }
                else
                {
                    builder.Append(']');
                }
            }
            return builder.ToString();
        }

        public UsingStatementNode(ScriptToken start, ScriptToken[] identifiers) : base(start)
        {
            Identifiers = identifiers;
        }
    }
}
