namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class CastExpressionNode : ExpressionNode
    {
        public ScriptToken TypeIdentifier { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public override CastExpressionNode Clone()
        {
            return new CastExpressionNode(Start, TypeIdentifier, Expression);
        }

        public override string ToString()
        {
            return "[<" + TypeIdentifier.Lexeme + '>' + Expression.ToString() + ']';
        }

        public CastExpressionNode(ScriptToken start, ScriptToken typeIdentifier, ExpressionNode expr) : base(start)
        {
            TypeIdentifier = typeIdentifier;
            Expression = expr;
        }
    }
}
