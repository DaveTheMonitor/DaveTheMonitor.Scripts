namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal class UnaryOperatorExpressionNode : ExpressionNode
    {
        public ScriptToken Operator { get; private set; }
        public ExpressionNode Operand { get; private set; }

        public override UnaryOperatorExpressionNode Clone()
        {
            return new UnaryOperatorExpressionNode(Start, Operator, Operand.Clone());
        }

        public override string ToString()
        {
            return $"{GetResultString()}[{Operator.Lexeme}{Operand}]";
        }

        public UnaryOperatorExpressionNode(ScriptToken start, ScriptToken op, ExpressionNode operand) : base(start)
        {
            Operator = op;
            Operand = operand;
        }
    }
}
