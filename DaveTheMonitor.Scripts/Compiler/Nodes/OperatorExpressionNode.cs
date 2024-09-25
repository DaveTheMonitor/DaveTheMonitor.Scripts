namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal class OperatorExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; private set; }
        public ScriptToken Operator { get; private set; }
        public ExpressionNode Right { get; private set; }

        public override OperatorExpressionNode Clone()
        {
            return new OperatorExpressionNode(Start, Left.Clone(), Operator, Right.Clone());
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{Left} {Operator.Lexeme} {Right}]";
        }

        public OperatorExpressionNode(ScriptToken start, ExpressionNode left, ScriptToken op, ExpressionNode right) : base(start)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
