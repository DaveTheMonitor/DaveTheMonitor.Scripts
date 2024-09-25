using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class CtorExpressionNode : ExpressionNode
    {
        public ScriptToken TypeIdentifier { get; private set; }
        public ExpressionNode[] Args { get; private set; }
        public ScriptMethod Ctor { get; set; }

        public override CtorExpressionNode Clone()
        {
            return new CtorExpressionNode(Start, TypeIdentifier, Clone(Args));
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{TypeIdentifier.Lexeme}{GetArgsString()}]";
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

        public CtorExpressionNode(ScriptToken start, ScriptToken typeIdentifier, ExpressionNode[] args) : base(start)
        {
            TypeIdentifier = typeIdentifier;
            Args = args;
            Ctor = null;
        }
    }
}
