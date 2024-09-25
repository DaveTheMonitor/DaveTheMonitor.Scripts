namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class ExitStatementNode : StatementNode
    {
        public override ExitStatementNode Clone()
        {
            return new ExitStatementNode(Start);
        }

        public override string ToString()
        {
            return "Exit";
        }

        public ExitStatementNode(ScriptToken start) : base(start)
        {
            
        }
    }
}
