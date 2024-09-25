using System.Text;

namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class FunctionStatementNode : StatementNode
    {
        public ScriptToken Name { get; private set; }
        public ScriptToken[] Args { get; private set; }
        public StatementNode[] Body { get; private set; }

        public override StatementNode Clone()
        {
            return new FunctionStatementNode(Start, Name, ScriptToken.Clone(Args), Clone(Body));
        }

        public override string ToString()
        {
            return $"Function {Name.Lexeme} {GetArgsString()}\n{GetBodyString()}\nEnd";
        }

        private string GetArgsString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Args.Length; i++)
            {
                builder.Append('[');
                builder.Append(Args[i].Lexeme);
                if (i < Args.Length - 1)
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

        private string GetBodyString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (StatementNode node in Body)
            {
                builder.AppendLine(node.ToString());
            }
            return builder.ToString();
        }

        public FunctionStatementNode(ScriptToken start, ScriptToken name, ScriptToken[] args, StatementNode[] body) : base(start)
        {
            Name = name;
            Args = args;
            Body = body;
        }
    }
}
