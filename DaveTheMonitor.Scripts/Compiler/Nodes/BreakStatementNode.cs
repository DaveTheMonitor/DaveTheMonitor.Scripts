namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class BreakStatementNode : StatementNode
    {
        public override BreakStatementNode Clone()
        {
            return new BreakStatementNode(Start);
        }

        public override string ToString()
        {
            return "Break";
        }

        public BreakStatementNode(ScriptToken start) : base(start)
        {
            
        }
    }
}
