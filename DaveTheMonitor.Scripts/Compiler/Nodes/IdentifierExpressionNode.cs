namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class IdentifierExpressionNode : ExpressionNode
    {
        public ScriptToken Identifier { get; private set; }

        public override IdentifierExpressionNode Clone()
        {
            return new IdentifierExpressionNode(Start, Identifier);
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{Identifier.Lexeme}]";
        }

        public IdentifierExpressionNode(ScriptToken start, ScriptToken identifier) : base(start)
        {
            Identifier = identifier;
        }
    }
}
