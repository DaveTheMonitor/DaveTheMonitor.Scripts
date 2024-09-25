namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal interface IMemberNode
    {
        public ScriptToken? ObjectIdentifier { get; }
        public ScriptToken Identifier { get; }
        public ExpressionNode[] Args { get; }
        public ScriptMember Member { get; set; }
    }
}
