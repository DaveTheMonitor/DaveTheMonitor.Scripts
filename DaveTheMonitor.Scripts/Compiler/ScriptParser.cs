using DaveTheMonitor.Scripts.Compiler.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class ScriptParser
    {
        public event ScriptCompilerErrorEventHandler ErrorHandler;

        private static readonly string[] _assignmentOperators =
        new string[] {
            "=",
            "+=",
            "-=",
            "*=",
            "/="
        };
        private ScriptToken[] _tokens;
        private int _index;
        private List<StatementNode> _statements;
        private bool _errored;

        public ScriptNode Parse()
        {
            if (_tokens.Length == 0)
            {
                return null;
            }
            _errored = false;
            _index = 0;

            ScriptToken? token = PeekToken();
            while (token.HasValue)
            {
                token = PeekNonEndLineToken();
                if (!token.HasValue)
                {
                    break;
                }

                if (!TryParseStatement(out StatementNode statement))
                {
                    _statements.Clear();
                    return null;
                }
                _statements.Add(statement);

                if (_index >= _tokens.Length)
                {
                    break;
                }
                token = PeekToken();
            }
            _index = 0;
            ScriptNode tree = new ScriptNode(_statements.ToArray());
            _statements.Clear();
            return tree;
        }

        private bool TryParseStatement(out StatementNode statement)
        {
            statement = ParseStatement();
            return statement != null;
        }

        private StatementNode ParseStatement()
        {
            if (_errored)
            {
                return null;
            }

            if (!TryPeekNonEndLineToken(out ScriptToken token))
            {
                Error(ScriptErrorCode.P_EndOfScript, "Unexpected End of Script", "Expected statement while parsing statement", PeekPrevToken());
                return null;
            }

            if (token.Type == ScriptTokenType.Keyword)
            {
                switch (token.Lexeme)
                {
                    case "using": return ParseUsingStatement();
                    case "in": return ParseInStatement();
                    case "var": return ParseVarStatement();
                    case "if": return ParseIfStatement();
                    case "while": return ParseWhileStatement();
                    case "for": return ParseForStatement();
                    case "foreach": return ParseForeachStatement();
                    case "loop": return ParseLoopStatement();
                    case "break": return ParseBreakStatement();
                    case "continue": return ParseContinueStatement();
                    case "return": return ParseReturnStatement();
                    case "exit": return ParseExitStatement();
                    case "function": return ParseFunctionStatement();
                    default:
                    {
                        Error(ScriptErrorCode.P_UnexpectedToken, "Unexpected Token", $"Unexpected {token.Lexeme}", token);
                        return null;
                    }
                }
            }
            else if (token.Type == ScriptTokenType.Identifier)
            {
                if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode expr))
                {
                    return null;
                }

                if (expr is MemberExpressionNode m)
                {
                    return new MemberStatementNode(token, m);
                }
                Error(ScriptErrorCode.P_UnexpectedToken, "Unexpected Token", $"Unexpected {token.Lexeme}", token);
                return null;
            }

            Error(ScriptErrorCode.P_UnexpectedToken, "Unexpected Token", $"Unexpected {token.Lexeme}", token);
            return null;
        }

        private UsingStatementNode ParseUsingStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            List<ScriptToken> identifiers = new List<ScriptToken>();
            ScriptToken next = PeekToken();
            while (next.Type != ScriptTokenType.EndLine)
            {
                if (NextToken().Type != ScriptTokenType.OpenBracket)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected [", PeekPrevToken());
                    return null;
                }

                ScriptToken identifier = NextToken();
                if (!IsIdentifier(identifier))
                {
                    Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", $"Expected identifier", identifier);
                    return null;
                }
                identifiers.Add(identifier);

                if (NextToken().Type != ScriptTokenType.ClosedBracket)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected ]", PeekPrevToken());
                    return null;
                }
                next = PeekToken();
            }

            return new UsingStatementNode(start, identifiers.ToArray());
        }

        private InStatementNode ParseInStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            List<InStatementNode.InIdentifier> identifiers = new List<InStatementNode.InIdentifier>();
            ScriptToken next = PeekToken();
            while (next.Type != ScriptTokenType.EndLine)
            {
                if (NextToken().Type != ScriptTokenType.OpenBracket)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekPrevToken());
                    return null;
                }

                ScriptToken firstIdentifier = NextToken();
                if (!IsIdentifier(firstIdentifier))
                {
                    Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", firstIdentifier);
                    return null;
                }

                next = NextToken();
                if (next.Type == ScriptTokenType.Colon)
                {
                    next = NextToken();
                    if (next.Type != ScriptTokenType.Identifier)
                    {
                        Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", next);
                        return null;
                    }
                    identifiers.Add(new InStatementNode.InIdentifier(firstIdentifier, next));
                    next = NextToken();
                    if (next.Type != ScriptTokenType.ClosedBracket)
                    {
                        Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected ]", next);
                        return null;
                    }
                    next = PeekToken();
                }
                else if (next.Type == ScriptTokenType.ClosedBracket)
                {
                    identifiers.Add(new InStatementNode.InIdentifier(null, firstIdentifier));
                    next = PeekToken();
                }
                else
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected : or ]", next);
                    return null;
                }
            }

            return new InStatementNode(start, identifiers.ToArray());
        }

        private StatementNode ParseVarStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (NextToken().Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekPrevToken());
                return null;
            }

            ScriptToken identifier = NextToken();
            if (!IsIdentifier(identifier))
            {
                Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", identifier);
                return null;
            }

            if (NextToken().Type != ScriptTokenType.ClosedBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected ]", PeekPrevToken());
                return null;
            }

            if (PeekToken().Type == ScriptTokenType.EndLine)
            {
                return new VarDeclarationStatementNode(start, identifier);
            }

            ScriptToken op = NextToken();
            if (IsIncrementOperator(op))
            {
                return new VarAssignmentStatementNode(start, identifier, op, null);
            }
            else if (!IsAssignmentOperator(op))
            {
                Error(ScriptErrorCode.P_ExpectedAssignment, "Unexpected Token", "Expected assignment, increment, or new line", op);
                return null;
            }

            if (PeekToken().Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekToken());
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode expr))
            {
                return null;
            }
            return new VarAssignmentStatementNode(start, identifier, op, expr);
        }

        private IfStatementNode ParseIfStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.OpenBracket && !IsUnaryOperator(next))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression", next);
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode condition))
            {
                return null;
            }

            if (!TryNextNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Then", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "then")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected Then", next);
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedBody, "Unexpected End of Script", "Expected If body", PeekPrevToken());
                return null;
            }
            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Type != ScriptTokenType.Keyword || (next.Lexeme != "else" && next.Lexeme != "elseif" && next.Lexeme != "end"))
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Else, ElseIf, or EndIf", PeekPrevToken());
                    return null;
                }
            }

            List<StatementNode> elseStatements = new List<StatementNode>();

            if (next.Lexeme == "elseif")
            {
                elseStatements.Add(ParseIfStatement());
            }
            else
            {
                NextNonEndLineToken();
                if (next.Lexeme == "else")
                {
                    if (!TryPeekNonEndLineToken(out next))
                    {
                        Error(ScriptErrorCode.P_ExpectedStatement, "Unexpected End of Script", "Expected statement", PeekPrevToken());
                        return null;
                    }

                    while (next.Type != ScriptTokenType.Keyword || next.Lexeme != "end")
                    {
                        if (!TryParseStatement(out StatementNode statement))
                        {
                            return null;
                        }
                        elseStatements.Add(statement);
                        if (!TryPeekNonEndLineToken(out next))
                        {
                            Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected EndIf", PeekPrevToken());
                            return null;
                        }
                    }
                    NextNonEndLineToken();
                    if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "end")
                    {
                        Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected EndIf", next);
                        return null;
                    }
                }
                else if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "end")
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected EndIf", next);
                    return null;
                }
            }

            return new IfStatementNode(start, condition, bodyStatements.ToArray(), elseStatements.Count > 0 ? elseStatements.ToArray() : null);
        }

        private WhileStatementNode ParseWhileStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression", next);
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode condition))
            {
                return null;
            }

            if (!TryNextNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Do", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "do")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected Do", next);
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                return null;
            }
            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Lexeme != "end")
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                    return null;
                }
            }
            NextNonEndLineToken();

            return new WhileStatementNode(start, condition, bodyStatements.ToArray());
        }

        private ForStatementNode ParseForStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedStatement, "Unexpected End of Script", "Expected statement", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword)
            {
                Error(ScriptErrorCode.P_ExpectedStatement, "Unexpected Token", "Expected statement", next);
                return null;
            }

            if (!TryParseStatement(out StatementNode initializer))
            {
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression", next);
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode condition))
            {
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedStatement, "Unexpected End of Script", "Expected statement", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword)
            {
                Error(ScriptErrorCode.P_ExpectedStatement, "Unexpected Token", "Expected statement", next);
                return null;
            }

            if (!TryParseStatement(out StatementNode iterator))
            {
                return null;
            }

            if (!TryNextNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Do", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "do")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected Do", next);
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                return null;
            }
            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Type != ScriptTokenType.Keyword || next.Lexeme != "end")
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                    return null;
                }
            }
            NextNonEndLineToken();

            return new ForStatementNode(start, initializer, condition, iterator, bodyStatements.ToArray());
        }

        private ForeachStatementNode ParseForeachStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Var", PeekPrevToken());
                return null;
            }

            NextNonEndLineToken();

            if (next.Type != ScriptTokenType.Keyword && next.Lexeme != "var")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected Var", next);
                return null;
            }

            if (NextToken().Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekPrevToken());
                return null;
            }

            ScriptToken identifier = NextToken();
            if (identifier.Type != ScriptTokenType.Identifier)
            {
                Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", next);
                return null;
            }

            if (NextToken().Type != ScriptTokenType.ClosedBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected ]", PeekPrevToken());
                return null;
            }

            next = NextToken();
            if (next.Type != ScriptTokenType.Keyword && next.Lexeme != "in")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected In", next);
                return null;
            }

            if (NextToken().Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekPrevToken());
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out ExpressionNode expr))
            {
                return null;
            }

            if (!TryNextNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected Do", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.Keyword || next.Lexeme != "do")
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected Token", "Expected Do", next);
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                return null;
            }
            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Type != ScriptTokenType.Keyword || next.Lexeme != "end")
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                    return null;
                }
            }
            NextNonEndLineToken();

            return new ForeachStatementNode(start, identifier, expr, bodyStatements.ToArray());
        }

        private LoopStatementNode ParseLoopStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression", PeekPrevToken());
                return null;
            }
            if (next.Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression", next);
                return null;
            }

            if (!TryParseExpression(ScriptTokenType.EndLine, "new line", out ExpressionNode count))
            {
                return null;
            }

            if (!TryPeekNonEndLineToken(out next))
            {
                Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                return null;
            }
            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Lexeme != "end")
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                    return null;
                }
            }
            NextNonEndLineToken();

            return new LoopStatementNode(start, count, bodyStatements.ToArray());
        }

        private BreakStatementNode ParseBreakStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;
            return new BreakStatementNode(start);
        }

        private ContinueStatementNode ParseContinueStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;
            return new ContinueStatementNode(start);
        }

        private ReturnStatementNode ParseReturnStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            ExpressionNode expr = null;
            if (PeekToken().Type != ScriptTokenType.EndLine && !TryParseExpression(ScriptTokenType.EndLine, "new line", out expr))
            {
                return null;
            }
            return new ReturnStatementNode(start, expr);
        }

        private ExitStatementNode ParseExitStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;
            return new ExitStatementNode(start);
        }

        private FunctionStatementNode ParseFunctionStatement()
        {
            ScriptToken start = NextNonEndLineToken().Value;

            ScriptToken name = NextToken();
            if (name.Type != ScriptTokenType.Identifier)
            {
                Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", name);
                return null;
            }
            if (!TryPeekNonEndLineToken(out ScriptToken next))
            {
                Error(ScriptErrorCode.P_ExpectedBody, "Unexpected End of Script", "Expected args or function body", PeekPrevToken());
                return null;
            }

            List<ScriptToken> args = new List<ScriptToken>();
            while (next.Type == ScriptTokenType.OpenBracket)
            {
                if (!TryNextNonEndLineToken(out ScriptToken t))
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected End of Script", "Expected [", PeekPrevToken());
                    return null;
                }
                if (t.Type != ScriptTokenType.OpenBracket)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [", PeekPrevToken());
                    return null;
                }

                ScriptToken identifier = NextToken();
                if (!IsIdentifier(identifier))
                {
                    Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", identifier);
                    return null;
                }
                args.Add(identifier);

                if (NextToken().Type != ScriptTokenType.ClosedBracket)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected ]", PeekPrevToken());
                    return null;
                }
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedBody, "Unexpected End of Script", "Expected arg or function body", PeekPrevToken());
                    return null;
                }
            }

            List<StatementNode> bodyStatements = new List<StatementNode>();
            while (next.Lexeme != "end")
            {
                if (!TryParseStatement(out StatementNode statement))
                {
                    return null;
                }
                bodyStatements.Add(statement);
                if (!TryPeekNonEndLineToken(out next))
                {
                    Error(ScriptErrorCode.P_ExpectedKeyword, "Unexpected End of Script", "Expected End", PeekPrevToken());
                    return null;
                }
            }
            NextToken();

            return new FunctionStatementNode(start, name, args.ToArray(), bodyStatements.ToArray());
        }

        private bool TryParseExpression(ScriptTokenType endToken, string endTokenName, out ExpressionNode expr)
        {
            expr = ParseExpression(endToken, endTokenName);
            return expr != null;
        }

        private ExpressionNode ParseExpression(ScriptTokenType endToken, string endTokenName)
        {
            // TODO: operator precedence
            if (_errored)
            {
                return null;
            }

            if (!TryNextNonEndLineToken(out ScriptToken start))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression, literal, or identifier", PeekPrevToken());
                return null;
            }
            ScriptToken left = start;
            ExpressionNode leftExpr = null;
            if (left.Type == ScriptTokenType.OpenBracket)
            {
                if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out leftExpr))
                {
                    return null;
                }
            }
            else if (left.Type == ScriptTokenType.Operator && left.Lexeme == "<")
            {
                ScriptToken castType = NextToken();
                if (castType.Type != ScriptTokenType.Identifier)
                {
                    Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", castType);
                    return null;
                }
                if (PeekToken().Type != ScriptTokenType.Operator || PeekToken().Lexeme != ">")
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected >", PeekToken());
                    return null;
                }
                NextToken();

                ExpressionNode castExpr;
                if (PeekToken().Type == ScriptTokenType.OpenBracket)
                {
                    NextToken();
                    if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out castExpr))
                    {
                        return null;
                    }
                }
                else if (PeekToken().Type == ScriptTokenType.Identifier)
                {
                    castExpr = new IdentifierExpressionNode(PeekToken(), PeekToken());
                    NextToken();
                }
                else
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", "Expected [ or identifier", PeekToken());
                    return null;
                }
                
                leftExpr = new CastExpressionNode(start, castType, castExpr);
            }
            else if (left.Type == ScriptTokenType.Keyword && left.Lexeme == "new")
            {
                ScriptToken typeIdentifier = NextToken();
                if (typeIdentifier.Type != ScriptTokenType.Identifier)
                {
                    Error(ScriptErrorCode.P_ExpectedIdentifier, "Unexpected Token", "Expected identifier", typeIdentifier);
                    return null;
                }

                List<ExpressionNode> args = new List<ExpressionNode>();
                while (PeekToken().Type == ScriptTokenType.OpenBracket)
                {
                    NextToken();
                    if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out ExpressionNode arg))
                    {
                        return null;
                    }
                    args.Add(arg);
                }

                if (NextToken().Type != endToken)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName}", PeekPrevToken());
                    return null;
                }

                return new CtorExpressionNode(start, typeIdentifier, args.ToArray());
            }
            else if (IsUnaryOperator(left))
            {
                ExpressionNode operand = null;
                if (PeekToken().Type == ScriptTokenType.OpenBracket)
                {
                    NextToken();
                    if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out operand))
                    {
                        return null;
                    }
                }
                else if (IsLiteral(PeekToken()))
                {
                    operand = new LiteralExpressionNode(start, NextToken());
                }
                else if (IsIdentifier(PeekToken()))
                {
                    operand = new IdentifierExpressionNode(start, NextToken());
                }
                else
                {
                    Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression, literal, or identifier", left);
                    return null;
                }

                if (NextToken().Type != endToken)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName}", PeekPrevToken());
                    return null;
                }

                return new UnaryOperatorExpressionNode(start, left, operand);
            }
            else if (!IsLiteral(left) && !IsIdentifier(left))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression, literal, or identifier", left);
                return null;
            }
            
            ScriptToken middle = PeekToken();
            if (middle.Type == endToken)
            {
                NextToken();
                if (leftExpr != null)
                {
                    return leftExpr;
                }
                else if (IsIdentifier(left))
                {
                    return new IdentifierExpressionNode(start, left);
                }
                else
                {
                    return new LiteralExpressionNode(start, left);
                }
            }
            else if (middle.Type == ScriptTokenType.Colon)
            {
                NextToken();
                ScriptToken identifier = NextToken();
                if (!IsIdentifier(identifier) && identifier.Type != endToken)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected identifier or {endTokenName}", identifier);
                    return null;
                }
                if (identifier.Type == endToken)
                {
                    return new MemberExpressionNode(start, null, left, null);
                }

                List<ExpressionNode> args = new List<ExpressionNode>();
                while (PeekToken().Type == ScriptTokenType.OpenBracket)
                {
                    NextToken();
                    if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out ExpressionNode arg))
                    {
                        return null;
                    }
                    args.Add(arg);
                }

                if (NextToken().Type != endToken)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName}", PeekPrevToken());
                    return null;
                }

                return new MemberExpressionNode(start, left, identifier, args.Count > 0 ? args.ToArray() : null);
            }
            else if (middle.Type == ScriptTokenType.OpenBracket)
            {
                List<ExpressionNode> args = new List<ExpressionNode>();
                while (PeekToken().Type == ScriptTokenType.OpenBracket)
                {
                    NextToken();
                    if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out ExpressionNode arg))
                    {
                        return null;
                    }
                    args.Add(arg);
                }
                if (NextToken().Type != endToken)
                {
                    Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName}", PeekPrevToken());
                    return null;
                }
                return new MemberExpressionNode(start, null, left, args.Count > 0 ? args.ToArray() : null);
            }
            else if (!IsNonAssignmentOperator(middle))
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName} or operator", middle);
                return null;
            }
            else if (leftExpr == null)
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Invalid Operation", "Left side of operator must be an expression", middle);
                return null;
            }
            NextToken();

            if (!TryNextNonEndLineToken(out ScriptToken right))
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected End of Script", "Expected expression", PeekPrevToken());
                return null;
            }
            if (right.Type != ScriptTokenType.OpenBracket)
            {
                Error(ScriptErrorCode.P_ExpectedExpr, "Unexpected Token", "Expected expression", right);
                return null;
            }
            if (!TryParseExpression(ScriptTokenType.ClosedBracket, "]", out ExpressionNode rightExpr))
            {
                return null;
            }

            ScriptToken end = NextToken();
            if (IsNonAssignmentOperator(end))
            {
                if (!TryParseExpression(endToken, endTokenName, out ExpressionNode r))
                {
                    return null;
                }
                rightExpr = new OperatorExpressionNode(rightExpr.Start, rightExpr, end, r);
            }
            else if (end.Type != endToken)
            {
                Error(ScriptErrorCode.P_ExpectedChar, "Unexpected Token", $"Expected {endTokenName} or operator", end);
                return null;
            }
            return new OperatorExpressionNode(leftExpr.Start, leftExpr, middle, rightExpr);
        }

        private ScriptToken? NextNonEndLineToken()
        {
            if (_index >= _tokens.Length)
            {
                return null;
            }

            ScriptToken? next = NextToken();
            while (next.HasValue && next.Value.Type == ScriptTokenType.EndLine)
            {
                if (_index >= _tokens.Length)
                {
                    return null;
                }
                next = NextToken();
            }
            return next;
        }

        private bool TryNextNonEndLineToken(out ScriptToken token)
        {
            ScriptToken? t = NextNonEndLineToken();
            token = t ?? default(ScriptToken);
            return t.HasValue;
        }

        private ScriptToken NextToken()
        {
            return _tokens[_index++];
        }

        private ScriptToken? PeekNonEndLineToken()
        {
            if (_index >= _tokens.Length)
            {
                return null;
            }

            ScriptToken next = PeekToken();
            int i = 1;
            while (next.Type == ScriptTokenType.EndLine)
            {
                if (_index + i >= _tokens.Length)
                {
                    return null;
                }
                next = PeekToken(i);
                i++;
            }
            return next;
        }

        private bool TryPeekNonEndLineToken(out ScriptToken token)
        {
            ScriptToken? t = PeekNonEndLineToken();
            token = t ?? default(ScriptToken);
            return t.HasValue;
        }

        private ScriptToken PeekToken()
        {
            return _tokens[_index];
        }

        private ScriptToken PeekToken(int offset)
        {
            return _tokens[_index + offset];
        }

        private ScriptToken PeekPrevToken()
        {
            return _tokens[_index - 1];
        }


        private bool IsAssignmentOperator(ScriptToken token)
        {
            return token.Type == ScriptTokenType.Operator && _assignmentOperators.Contains(token.Lexeme);
        }

        private bool IsIncrementOperator(ScriptToken token)
        {
            return token.Type == ScriptTokenType.Operator && (token.Lexeme == "++" || token.Lexeme == "--");
        }

        private bool IsNonAssignmentOperator(ScriptToken token)
        {
            return token.Type == ScriptTokenType.Operator && !_assignmentOperators.Contains(token.Lexeme);
        }

        private bool IsLiteral(ScriptToken token)
        {
            return token.Type == ScriptTokenType.NullLiteral || token.Type == ScriptTokenType.NumLiteral || token.Type == ScriptTokenType.StringLiteral || token.Type == ScriptTokenType.TrueLiteral || token.Type == ScriptTokenType.FalseLiteral;
        }

        private bool IsIdentifier(ScriptToken token)
        {
            return token.Type == ScriptTokenType.Identifier;
        }

        private bool IsUnaryOperator(ScriptToken token)
        {
            return token.Type == ScriptTokenType.Operator && (token.Lexeme == "+" || token.Lexeme == "-" || token.Lexeme == "!" || token.Lexeme == "not");
        }

        private void Error(ScriptErrorCode code, string header, string message, ScriptToken token)
        {
            Error(code, header, message, token.Pos);
        }

        private void Error(ScriptErrorCode code, string header, string message, int pos)
        {
            _errored = true;
            ErrorHandler?.Invoke(this, new ScriptCompilerErrorEventArgs(code, header, message, pos, ScriptErrorSeverity.Error));
        }

        public void SetTokens(ScriptToken[] tokens)
        {
            _tokens = tokens;
        }

        public ScriptParser()
        {
            _tokens = null;
            _statements = new List<StatementNode>();
            _index = 0;
        }
    }
}
