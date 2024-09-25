namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal sealed class VarAssignmentStatementNode : StatementNode
    {
        public ScriptToken Identifier { get; private set; }
        public ScriptToken Operator { get; private set; }
        public ExpressionNode Expression { get; private set; }
        public ScriptType OrigType { get; set; }

        public override VarAssignmentStatementNode Clone()
        {
            return new VarAssignmentStatementNode(Start, Identifier, Operator, Expression?.Clone());
        }

        public override string ToString()
        {
            return $"Var [{Identifier.Lexeme}] {Operator.Lexeme} {Expression}";
        }

        public VarAssignmentStatementNode(ScriptToken start, ScriptToken identifier, ScriptToken op, ExpressionNode expression) : base(start)
        {
            Identifier = identifier;
            Operator = op;
            Expression = expression;
            OrigType = null;
        }
    }
}
