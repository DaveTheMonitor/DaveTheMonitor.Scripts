namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class LiteralExpressionNode : ExpressionNode
    {
        public ScriptToken Literal { get; private set; }
        public object Value { get; set; }

        public override LiteralExpressionNode Clone()
        {
            return new LiteralExpressionNode(Start, Literal);
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{Literal.Lexeme}]";
        }

        public LiteralExpressionNode(ScriptToken start, ScriptToken literal) : base(start)
        {
            Literal = literal;
        }
    }
}
