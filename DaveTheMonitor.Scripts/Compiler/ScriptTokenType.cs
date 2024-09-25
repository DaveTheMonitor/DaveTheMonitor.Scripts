namespace DaveTheMonitor.Scripts.Compiler
{
    internal enum ScriptTokenType
    {
        Keyword,
        Identifier,
        Operator,
        NullLiteral,
        NumLiteral,
        StringLiteral,
        TrueLiteral,
        FalseLiteral,
        OpenBracket,
        ClosedBracket,
        EndLine,
        Colon,
        Comment
    }
}
