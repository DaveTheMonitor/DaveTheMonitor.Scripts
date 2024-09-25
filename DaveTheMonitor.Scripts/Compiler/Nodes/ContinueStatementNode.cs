namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ContinueStatementNode : StatementNode
    {
        public override ContinueStatementNode Clone()
        {
            return new ContinueStatementNode(Start);
        }


        public override string ToString()
        {
            return "Continue";
        }

        public ContinueStatementNode(ScriptToken start) : base(start)
        {
            
        }
    }
}
