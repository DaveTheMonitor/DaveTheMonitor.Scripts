using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class MemberExpressionNode : ExpressionNode, IMemberNode
    {
        public ScriptToken? ObjectIdentifier { get; private set; }
        public ScriptToken Identifier { get; private set; }
        public ExpressionNode[] Args { get; private set; }
        public ScriptMember Member { get; set; }

        public override MemberExpressionNode Clone()
        {
            return new MemberExpressionNode(Start, ObjectIdentifier, Identifier, Clone(Args));
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{(ObjectIdentifier.HasValue ? $"{ObjectIdentifier.Value.Lexeme}:" : "func:")}{Identifier.Lexeme}{GetArgsString()}]";
        }

        private string GetArgsString()
        {
            if (Args == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder(" ");
            for (int i = 0; i < Args.Length; i++)
            {
                builder.Append(Args[i].ToString());
                if (i < Args.Length - 1)
                {
                    builder.Append(' ');
                }
            }
            return builder.ToString();
        }

        public MemberExpressionNode(ScriptToken start, ScriptToken? objectIdentifier, ScriptToken identifier, ExpressionNode[] args) : base(start)
        {
            ObjectIdentifier = objectIdentifier;
            Identifier = identifier;
            Args = args;
            Member = null;
        }
    }
}
