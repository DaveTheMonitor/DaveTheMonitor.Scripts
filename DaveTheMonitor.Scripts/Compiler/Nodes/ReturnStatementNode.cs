namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ReturnStatementNode : StatementNode
    {
        public ExpressionNode Expression;

        public override ReturnStatementNode Clone()
        {
            return new ReturnStatementNode(Start, Expression.Clone());
        }

        public override string ToString()
        {
            return "Return " + Expression.ToString();
        }

        public ReturnStatementNode(ScriptToken start, ExpressionNode expr) : base(start)
        {
            Expression = expr;
        }
    }
}
