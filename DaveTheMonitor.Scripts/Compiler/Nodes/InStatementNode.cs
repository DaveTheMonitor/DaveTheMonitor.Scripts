using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class InStatementNode : StatementNode
    {
        public struct InIdentifier
        {
            public ScriptType Type { get; set; }
            public ScriptToken? TypeIdentifier { get; set; }
            public ScriptToken Identifier { get; set; }

            public static InIdentifier[] Clone(InIdentifier[] array)
            {
                InIdentifier[] nodes = new InIdentifier[array.Length];
                array.CopyTo(nodes, 0);
                return nodes;
            }

            public InIdentifier(ScriptToken? typeIdentifier, ScriptToken identifier)
            {
                Type = null;
                TypeIdentifier = typeIdentifier;
                Identifier = identifier;
            }
        }
        public InIdentifier[] Identifiers { get; private set; }

        public override InStatementNode Clone()
        {
            return new InStatementNode(Start, InIdentifier.Clone(Identifiers));
        }

        public override string ToString()
        {
            return $"In {GetIdentifiersString()}";
        }

        private string GetIdentifiersString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Identifiers.Length; i++)
            {
                builder.Append('[');
                if (Identifiers[i].TypeIdentifier.HasValue)
                {
                    builder.Append(Identifiers[i].TypeIdentifier.Value.Lexeme);
                    builder.Append(':');
                }
                builder.Append(Identifiers[i].Identifier.Lexeme);
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

        public InStatementNode(ScriptToken start, InIdentifier[] identifiers) : base(start)
        {
            Identifiers = identifiers;
        }
    }
}
