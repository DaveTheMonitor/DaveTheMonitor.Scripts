namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class VarDeclarationStatementNode : StatementNode
    {
        public ScriptToken Identifier { get; private set; }

        public override VarDeclarationStatementNode Clone()
        {
            return new VarDeclarationStatementNode(Start, Identifier);
        }

        public override string ToString()
        {
            return $"Var [{Identifier.Lexeme}]";
        }

        public VarDeclarationStatementNode(ScriptToken start, ScriptToken identifier) : base(start)
        {
            Identifier = identifier;
        }
    }
}
