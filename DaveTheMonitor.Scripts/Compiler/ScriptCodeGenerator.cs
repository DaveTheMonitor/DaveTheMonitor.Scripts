using DaveTheMonitor.Scripts.Compiler.Nodes;
using DaveTheMonitor.Scripts.Runtime;
using System;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class ScriptCodeGenerator
    {
        private class Context
        {
            public ContextType Type { get; private set; }
            public List<int> ContextBreaks { get; private set; }
            public StatementNode Statement { get; private set; }
            public int Start { get; private set; }
            public Context Parent { get; private set; }
            private Dictionary<int, ScriptType> _localTypes;

            public ScriptType GetLocalType(int local)
            {
                if (_localTypes.TryGetValue(local, out ScriptType type))
                {
                    return type;
                }

                if (Parent != null)
                {
                    return Parent.GetLocalType(local);
                }
                return ScriptType.Void;
            }

            public void Close()
            {
                CopyLocalTypesTo(Parent);
            }

            private void CopyLocalTypesTo(Context context)
            {
                foreach (KeyValuePair<int, ScriptType> pair in _localTypes)
                {
                    if (context._localTypes.TryGetValue(pair.Key, out ScriptType current) && current != pair.Value)
                    {
                        context._localTypes[pair.Key] = ScriptType.ScriptVar;
                    }
                }
            }

            public void SetLocalType(int index, ScriptType type)
            {
                _localTypes[index] = type;
            }

            public Context(ContextType type, StatementNode statement, int start, Context parent)
            {
                Type = type;
                ContextBreaks = new List<int>();
                Statement = statement;
                Start = start;
                Parent = parent;
                _localTypes = new Dictionary<int, ScriptType>();
            }
        }

        private enum ContextType
        {
            Global,
            Condition,
            Loop
        }

        public event ScriptCompilerErrorEventHandler ErrorHandler;

        private ScriptNode _tree;
        private ScriptGenerator _gen;
        private List<Symbol> _locals;
        private Dictionary<string, ScriptInVarDefinition> _inVars;
        private SymbolTable _symbolTable;
        private List<Context> _contextStack;
        private Stack<int> _tempVarIndexes;
        private Stack<int> _loopCounters;
        private int _maxStack;
        private bool _errored;

        public Script Compile(string name, ScriptRuntimeType type)
        {
            _errored = false;
            _gen = new ScriptGenerator(name);
            _locals.Clear();
            _inVars.Clear();
            _contextStack.Clear();
            _tempVarIndexes.Clear();
            PushContext(ContextType.Global, null);
            _maxStack = -1;

            foreach (StatementNode node in _tree.Children)
            {
                Compile(node);
                if (_errored)
                {
                    break;
                }
            }
            _gen.Write(ScriptOp.Exit);

            _contextStack.Clear();
            return _gen.CreateScript(type, _maxStack, _inVars);
        }

        private void Compile(StatementNode node)
        {
            if (_errored)
            {
                return;
            }

            if (node is UsingStatementNode)
            {
                // using nodes are only used in semantic analysis
                return;
            }
            else if (node is InStatementNode inNode)
            {
                Compile(inNode);
                return;
            }
            else if (node is VarAssignmentStatementNode varNode)
            {
                Compile(varNode);
            }
            else if (node is VarDeclarationStatementNode varDecNode)
            {
                Compile(varDecNode);
            }
            else if (node is IfStatementNode ifNode)
            {
                Compile(ifNode);
            }
            else if (node is WhileStatementNode whileNode)
            {
                Compile(whileNode);
            }
            else if (node is ForStatementNode forNode)
            {
                Compile(forNode);
            }
            else if (node is ForeachStatementNode foreachNode)
            {
                Compile(foreachNode);
            }
            else if (node is LoopStatementNode loopNode)
            {
                Compile(loopNode);
            }
            else if (node is BreakStatementNode breakNode)
            {
                Compile(breakNode);
            }
            else if (node is ContinueStatementNode continueNode)
            {
                Compile(continueNode);
            }
            else if (node is ReturnStatementNode returnNode)
            {
                Compile(returnNode);
            }
            else if (node is ExitStatementNode exitNode)
            {
                Compile(exitNode);
            }
            else if (node is FunctionStatementNode functionNode)
            {
                Compile(functionNode);
            }
            else if (node is MemberStatementNode memberNode)
            {
                Compile(memberNode);
            }
            else
            {
                throw new Exception("Invalid node in code generator " + node.GetType().ToString());
            }
        }

        private void Compile(InStatementNode node)
        {
            foreach (InStatementNode.InIdentifier identifier in node.Identifiers)
            {
                int index = GetSymbolLocalIndex(identifier.Identifier);
                _inVars.Add(identifier.Identifier.Lexeme, new ScriptInVarDefinition(identifier.Identifier.Lexeme, identifier.Type, index));
            }
        }

        private void Compile(VarAssignmentStatementNode node)
        {
            int index = GetSymbolLocalIndex(node.Identifier);
            switch (node.Operator.Lexeme)
            {
                case "=": Compile(node.Expression); break;
                case "++":
                {
                    WriteLoadLoc(index);
                    _gen.WriteLiteral(1);
                    _gen.Write(ScriptOp.Add_Num);
                    break;
                }
                case "--":
                {
                    WriteLoadLoc(index);
                    _gen.WriteLiteral(1);
                    _gen.Write(ScriptOp.Sub_Num);
                    break;
                }
                case "+=":
                {
                    WriteLoadLoc(index);
                    Compile(node.Expression);
                    bool num = IsNum(node.OrigType) && IsNum(node.Expression.ResultType);
                    _gen.Write(num ? ScriptOp.Add_Num : ScriptOp.Add);
                    break;
                }
                case "-=":
                {
                    WriteLoadLoc(index);
                    Compile(node.Expression);
                    bool num = IsNum(node.OrigType) && IsNum(node.Expression.ResultType);
                    _gen.Write(num ? ScriptOp.Sub_Num : ScriptOp.Sub);
                    break;
                }
                case "*=":
                {
                    WriteLoadLoc(index);
                    Compile(node.Expression);
                    bool num = IsNum(node.OrigType) && IsNum(node.Expression.ResultType);
                    _gen.Write(num ? ScriptOp.Mul_Num : ScriptOp.Mul);
                    break;
                }
                case "/=":
                {
                    WriteLoadLoc(index);
                    Compile(node.Expression);
                    bool num = IsNum(node.OrigType) && IsNum(node.Expression.ResultType);
                    _gen.Write(num ? ScriptOp.Div_Num : ScriptOp.Div);
                    break;
                }
                default:
                {
                    throw new Exception("Invalid operator");
                }
            }
            WriteSetLoc(index, node.Expression?.ResultType ?? node.OrigType);
        }

        private void Compile(VarDeclarationStatementNode node)
        {
            int index = GetSymbolLocalIndex(node.Identifier);
            _gen.WriteNullLiteral();
            WriteSetLoc(index, ScriptType.ScriptVar);
        }

        private void Compile(IfStatementNode node)
        {
            PushContext(ContextType.Condition, node);

            Compile(node.Condition);
            if (node.Condition.ResultType.Id == ScriptType.ScriptVar.Id)
            {
                _gen.WriteLiteral(true);
                _gen.Write(ScriptOp.Eq);
            }

            _gen.Write(ScriptOp.JumpF);
            PeekContext().ContextBreaks.Add(_gen.Position);
            _gen.Write(-1);

            foreach (StatementNode st in node.Body)
            {
                Compile(st);
            }

            int endPos = -1;
            if (node.Else != null)
            {
                _gen.Write(ScriptOp.Jump);
                endPos = _gen.Position;
                _gen.Write(-1);
            }

            PopContext();

            if (node.Else != null)
            {
                foreach (StatementNode st in node.Else)
                {
                    Compile(st);
                }

                int pos = _gen.Position;
                _gen.Position = endPos;
                _gen.Write(pos);
                _gen.Position = pos;
            }
        }

        private void Compile(WhileStatementNode node)
        {
            PushContext(ContextType.Loop, node);
            int start = _gen.Position;

            Compile(node.Condition);
            if (node.Condition.ResultType == ScriptType.ScriptVar)
            {
                _gen.WriteLiteral(true);
                _gen.Write(ScriptOp.Eq);
            }

            _gen.Write(ScriptOp.JumpF);
            PeekContext().ContextBreaks.Add(_gen.Position);
            _gen.Write(-1);

            foreach (StatementNode st in node.Body)
            {
                Compile(st);
            }

            _gen.Write(ScriptOp.Jump);
            _gen.Write(start);

            PopContext();
        }

        private void Compile(ForStatementNode node)
        {
            Compile(node.Initializer);

            PushContext(ContextType.Loop, node);
            int start = _gen.Position;

            Compile(node.Condition);
            if (node.Condition.ResultType == ScriptType.ScriptVar)
            {
                _gen.WriteLiteral(true);
                _gen.Write(ScriptOp.Eq);
            }

            _gen.Write(ScriptOp.JumpF);
            PeekContext().ContextBreaks.Add(_gen.Position);
            _gen.Write(-1);

            foreach (StatementNode st in node.Body)
            {
                Compile(st);
            }

            Compile(node.Iterator);

            _gen.Write(ScriptOp.Jump);
            _gen.Write(start);

            PopContext();
        }

        private void Compile(ForeachStatementNode node)
        {
            _gen.WriteLiteral(0);

            Compile(node.Iterator);
            int enumerableIndex = GetTempVarIndex();
            if (enumerableIndex == -1)
            {
                string tempCounter = "temp_" + _gen.Position.ToString();
                _symbolTable.AddSymbol(new Symbol(tempCounter));
                enumerableIndex = GetSymbolLocalIndex(_symbolTable.GetSymbol(tempCounter));
            }
            WriteSetLoc(enumerableIndex, node.Iterator.ResultType);

            int counterIndex = GetTempVarIndex();
            if (counterIndex == -1)
            {
                string tempCounter = "temp_" + _gen.Position.ToString();
                _symbolTable.AddSymbol(new Symbol(tempCounter));
                counterIndex = GetSymbolLocalIndex(_symbolTable.GetSymbol(tempCounter));
            }
            WriteSetLoc(counterIndex, ScriptType.Long);
            _loopCounters.Push(counterIndex);

            PushContext(ContextType.Loop, node);
            int start = _gen.Position;

            WriteLoadLoc(counterIndex);

            WriteLoadLoc(enumerableIndex);

            ScriptType type = node.Iterator.ResultType;
            if (type.IteratorCount is ScriptMethod method)
            {
                _gen.WriteInvoke(method);
            }
            else if (type.IteratorCount is ScriptProperty prop)
            {
                _gen.WriteGetProperty(prop);
            }
            else
            {
                throw new Exception("Invalid iterator");
            }

            _gen.Write(ScriptOp.JumpGte);
            PeekContext().ContextBreaks.Add(_gen.Position);
            _gen.Write(-1);

            WriteLoadLoc(enumerableIndex);
            WriteLoadLoc(counterIndex);
            _gen.WriteInvoke(type.IteratorGetItem);
            WriteSetLoc(GetSymbolLocalIndex(node.ItemIdentifier), type.IteratorGetItem.ReturnType);

            foreach (StatementNode st in node.Body)
            {
                Compile(st);
            }

            WriteLoadLoc(counterIndex);
            _gen.WriteLiteral(1);
            _gen.Write(ScriptOp.Add);
            WriteSetLoc(counterIndex, ScriptType.Long);

            _gen.Write(ScriptOp.Jump);
            _gen.Write(start);

            PopContext();

            _loopCounters.Pop();
            _tempVarIndexes.Push(counterIndex);
        }

        private void Compile(LoopStatementNode node)
        {
            // TODO: loop opcode instead of for?

            Compile(node.Count);
            if (node.Count.ResultType != ScriptType.Long)
            {
                _gen.Write(ScriptOp.PeekCheckType, ScriptType.Long.Id);
            }

            int countIndex = GetTempVarIndex();
            if (countIndex == -1)
            {
                string tempCount = "temp_" + _gen.Position.ToString();
                _symbolTable.AddSymbol(new Symbol(tempCount));
                countIndex = GetSymbolLocalIndex(_symbolTable.GetSymbol(tempCount));
            }
            WriteSetLoc(countIndex, ScriptType.Long);

            _gen.WriteLiteral(0);

            int counterIndex = GetTempVarIndex();
            if (counterIndex == -1)
            {
                string tempCounter = "temp_" + _gen.Position.ToString();
                _symbolTable.AddSymbol(new Symbol(tempCounter));
                counterIndex = GetSymbolLocalIndex(_symbolTable.GetSymbol(tempCounter));
            }
            WriteSetLoc(counterIndex, ScriptType.Long);
            _loopCounters.Push(counterIndex);

            PushContext(ContextType.Loop, node);
            int start = _gen.Position;

            WriteLoadLoc(counterIndex);
            WriteLoadLoc(countIndex);
            _gen.Write(ScriptOp.JumpGte);
            PeekContext().ContextBreaks.Add(_gen.Position);
            _gen.Write(-1);

            foreach (StatementNode st in node.Body)
            {
                Compile(st);
            }

            WriteLoadLoc(counterIndex);
            _gen.WriteLiteral(1);
            _gen.Write(ScriptOp.Add);
            WriteSetLoc(counterIndex, ScriptType.Long);

            _gen.Write(ScriptOp.Jump);
            _gen.Write(start);

            PopContext();

            _loopCounters.Pop();
            _tempVarIndexes.Push(counterIndex);
            _tempVarIndexes.Push(countIndex);
        }

        private void Compile(BreakStatementNode node)
        {
            _gen.Write(ScriptOp.Jump);
            for (int i = _contextStack.Count - 1; i >= 0; i--)
            {
                Context context = _contextStack[i];
                if (context.Type == ContextType.Loop)
                {
                    context.ContextBreaks.Add(_gen.Position);
                    break;
                }
            }
            _gen.Write(-1);
        }

        private void Compile(ContinueStatementNode node)
        {
            for (int i = _contextStack.Count - 1; i >= 0; i--)
            {
                Context context = _contextStack[i];
                if (context.Type == ContextType.Loop)
                {
                    if (context.Statement is ForStatementNode forNode)
                    {
                        Compile(forNode.Iterator);
                    }
                    else if (context.Statement is LoopStatementNode or ForeachStatementNode)
                    {
                        int counterIndex = _loopCounters.Peek();

                        WriteLoadLoc(counterIndex);
                        _gen.WriteLiteral(1);
                        _gen.Write(ScriptOp.Add_Num);
                        WriteSetLoc(counterIndex, ScriptType.Long);
                    }
                    _gen.Write(ScriptOp.Jump);
                    _gen.Write(context.Start);
                    break;
                }
            }
        }

        private void Compile(ReturnStatementNode node)
        {
            if (node.Expression != null)
            {
                Compile(node.Expression);
                _gen.Write(ScriptOp.Return);
            }
            else
            {
                _gen.Write(ScriptOp.Exit);
            }
        }

        private void Compile(ExitStatementNode node)
        {
            _gen.Write(ScriptOp.Exit);
        }

        private void Compile(FunctionStatementNode node)
        {
            Error(ScriptErrorCode.C_UnsupportedStatement, "Unsupported Statement", "Function is not supported.", node.Name, ScriptErrorSeverity.Error);
        }

        private void Compile(MemberStatementNode node)
        {
            Compile((IMemberNode)node);
            if (node.Member?.Name != "print")
            {
                _gen.Write(ScriptOp.Pop);
            }
        }

        private void Compile(ExpressionNode expr)
        {
            if (_errored)
            {
                return;
            }

            if (expr is CastExpressionNode castExpr)
            {
                Compile(castExpr);
            }
            else if (expr is IdentifierExpressionNode identifierExpr)
            {
                Compile(identifierExpr);
            }
            else if (expr is LiteralExpressionNode litExpr)
            {
                Compile(litExpr);
            }
            else if (expr is MemberExpressionNode memberExpr)
            {
                Compile(memberExpr);
            }
            else if (expr is CtorExpressionNode ctorExpr)
            {
                Compile(ctorExpr);
            }
            else if (expr is OperatorExpressionNode operatorExpr)
            {
                Compile(operatorExpr);
            }
            else if (expr is UnaryOperatorExpressionNode unaryExpr)
            {
                Compile(unaryExpr);
            }
            else
            {
                throw new Exception("Invalid node in code generator " + expr.GetType().ToString());
            }
        }

        private void Compile(CastExpressionNode expr)
        {
            Error(ScriptErrorCode.C_UnsupportedExpr, "Unsupported Expression", "Expression is not supported.", expr.TypeIdentifier, ScriptErrorSeverity.Error);
        }

        private void Compile(IdentifierExpressionNode expr)
        {
            WriteLoadLoc(GetSymbolLocalIndex(expr.Identifier));
        }

        private void Compile(LiteralExpressionNode expr)
        {
            ScriptType type = expr.ResultType;
            if (type == ScriptType.Double)
            {
                _gen.WriteLiteral((double)expr.Value);
            }
            else if (type == ScriptType.Long)
            {
                _gen.WriteLiteral((long)expr.Value);
            }
            else if (type == ScriptType.Bool)
            {
                _gen.WriteLiteral((bool)expr.Value);
            }
            else if (type == ScriptType.String)
            {
                _gen.WriteLiteral((string)expr.Value);
            }
            else if (type == ScriptType.ScriptVar && expr.Literal.Lexeme == "null")
            {
                _gen.WriteNullLiteral();
            }
        }

        private void Compile(MemberExpressionNode expr)
        {
            Compile((IMemberNode)expr);
        }

        private void Compile(IMemberNode node)
        {
            if (node.Member != null)
            {
                if (!node.Member.IsStatic)
                {
                    WriteLoadLoc(GetSymbolLocalIndex(node.ObjectIdentifier.Value));
                }

                if (node.Member is ScriptMethod method)
                {
                    if (method.Name == "print")
                    {
                        Compile(node.Args[0]);
                        _gen.Write(ScriptOp.Print);
                        return;
                    }

                    if (node.Args?.Length > 0)
                    {
                        for (int i = 0; i < node.Args.Length; i++)
                        {
                            ExpressionNode arg = node.Args[i];
                            Compile(arg);
                            if (arg.ResultType == ScriptType.ScriptVar)
                            {
                                _gen.Write(ScriptOp.PeekCheckType, method.Args[i].Id);
                            }
                        }
                    }
                    _gen.WriteInvoke(method);
                }
                else if (node.Member is ScriptProperty prop)
                {
                    _gen.WriteGetProperty(prop);
                }
                else
                {
                    ScriptConst constant = (ScriptConst)node.Member;
                    if (constant.Type == ScriptType.Long)
                    {
                        _gen.WriteLiteral((long)constant.Value);
                    }
                    else if (constant.Type == ScriptType.Double)
                    {
                        _gen.WriteLiteral((double)constant.Value);
                    }
                    else if (constant.Type == ScriptType.Bool)
                    {
                        _gen.WriteLiteral((bool)constant.Value);
                    }
                    else if (constant.Type == ScriptType.String)
                    {
                        _gen.WriteLiteral((string)constant.Value);
                    }
                    else
                    {
                        throw new Exception("Invalid constant type");
                    }
                }
            }
            else
            {
                WriteLoadLoc(GetSymbolLocalIndex(node.ObjectIdentifier.Value));

                int args = 0;
                if (node.Args != null)
                {
                    foreach (ExpressionNode arg in node.Args)
                    {
                        Compile(arg);
                    }
                    args = node.Args.Length;
                }
                _gen.WriteInvokeDynamic(node.Identifier.Lexeme, args);
            }
        }

        private void Compile(CtorExpressionNode expr)
        {
            if (expr.Args.Length > 0)
            {
                for (int i = 0; i < expr.Args.Length; i++)
                {
                    ExpressionNode arg = expr.Args[i];
                    Compile(arg);
                    if (arg.ResultType == ScriptType.ScriptVar)
                    {
                        _gen.Write(ScriptOp.PeekCheckType, expr.Ctor.Args[i].Id);
                    }
                }
            }
            _gen.WriteInvoke(expr.Ctor);
        }

        private void Compile(OperatorExpressionNode expr)
        {
            Compile(expr.Left);
            Compile(expr.Right);
            WriteOperator(expr.Operator, expr.Left.ResultType, expr.Right.ResultType);
        }

        private void Compile(UnaryOperatorExpressionNode expr)
        {
            if (expr.Operator.Lexeme == "+")
            {
                Compile(expr.Operand);
                if (expr.ResultType == ScriptType.ScriptVar)
                {
                    _gen.Write(ScriptOp.PeekCheckType, ScriptType.Double);
                }
            }
            else if (expr.Operator.Lexeme == "-")
            {
                if (expr.Operand is LiteralExpressionNode lit)
                {
                    if (lit.ResultType == ScriptType.Double)
                    {
                        _gen.WriteLiteral(-(double)lit.Value);
                    }
                    else
                    {
                        _gen.WriteLiteral(-(long)lit.Value);
                    }
                    return;
                }

                Compile(expr.Operand);
                if (expr.ResultType == ScriptType.ScriptVar)
                {
                    _gen.Write(ScriptOp.Neg);
                }
                else
                {
                    _gen.Write(ScriptOp.Neg_Num);
                }
            }
            else if (expr.Operator.Lexeme == "!" || expr.Operator.Lexeme == "not")
            {
                if (expr.Operand is LiteralExpressionNode lit)
                {
                    _gen.WriteLiteral(!(bool)lit.Value);
                    return;
                }

                Compile(expr.Operand);
                if (expr.ResultType == ScriptType.ScriptVar)
                {
                    _gen.Write(ScriptOp.Invert);
                }
                else
                {
                    _gen.Write(ScriptOp.Invert_Bool);
                }
            }
        }

        private void PushContext(ContextType type, StatementNode statement)
        {
            _contextStack.Add(new Context(type, statement, _gen.Position, _contextStack.Count > 0 ? PeekContext() : null));
        }

        private void PopContext()
        {
            Context context = _contextStack[_contextStack.Count - 1];
            context.Close();
            _contextStack.RemoveAt(_contextStack.Count - 1);
            if (context.ContextBreaks.Count > 0)
            {
                int pos = _gen.Position;
                foreach (int contextBreak in context.ContextBreaks)
                {
                    _gen.Position = contextBreak;
                    _gen.Write(pos);
                }
                _gen.Position = pos;
            }
        }

        private Context PeekContext()
        {
            return _contextStack[_contextStack.Count - 1];
        }

        private void WriteOperator(ScriptToken op, ScriptType left, ScriptType right)
        {
            bool str = left == ScriptType.String && right == ScriptType.String;
            bool num = IsNum(left) && IsNum(right);
            switch (op.Lexeme)
            {
                case "+": _gen.Write(str ? ScriptOp.AddStr : num ? ScriptOp.Add_Num : ScriptOp.Add); break;
                case "-": _gen.Write(num ? ScriptOp.Sub_Num : ScriptOp.Sub); break;
                case "*": _gen.Write(num ? ScriptOp.Mul_Num : ScriptOp.Mul); break;
                case "/": _gen.Write(num ? ScriptOp.Div_Num : ScriptOp.Div); break;
                case "%": _gen.Write(num ? ScriptOp.Mod_Num : ScriptOp.Mod); break;
                case "<": _gen.Write(num ? ScriptOp.Lt_Num : ScriptOp.Lt); break;
                case "<=": _gen.Write(num ? ScriptOp.Lte_Num : ScriptOp.Lte); break;
                case ">": _gen.Write(num ? ScriptOp.Gt_Num : ScriptOp.Gt); break;
                case ">=": _gen.Write(num ? ScriptOp.Gte_Num : ScriptOp.Gte); break;
                case "==": _gen.Write(ScriptOp.Eq); break;
                case "!=": _gen.Write(ScriptOp.Neq); break;
                case "and": _gen.Write(ScriptOp.And); break;
                case "&&": _gen.Write(ScriptOp.And); break;
                case "or": _gen.Write(ScriptOp.Or); break;
                case "||": _gen.Write(ScriptOp.Or); break;
                default: throw new Exception("Invalid operator");
            }
        }

        private bool IsNum(ScriptType type)
        {
            return type == ScriptType.Long || type == ScriptType.Double;
        }

        private int GetSymbolLocalIndex(Symbol symbol)
        {
            int index = _locals.IndexOf(symbol);
            if (index == -1)
            {
                index = _locals.Count;
                _locals.Add(symbol);
            }
            return index;
        }

        private int GetTempVarIndex()
        {
            if (_tempVarIndexes.Count == 0)
            {
                return -1;
            }
            return _tempVarIndexes.Pop();
        }

        private int GetSymbolLocalIndex(ScriptToken identifier)
        {
            return GetSymbolLocalIndex(GetSymbol(identifier));
        }

        private Symbol GetSymbol(ScriptToken identifier)
        {
            return _symbolTable.GetSymbol(identifier.Lexeme);
        }

        private void WriteSetLoc(int index, ScriptType type)
        {
            ScriptType prev = PeekContext().GetLocalType(index);
            PeekContext().SetLocalType(index, type);
            if (prev.MaybeRef)
            {
                _gen.WriteSetLoc((byte)index);
            }
            else
            {
                _gen.WriteSetLocNoRef((byte)index);
            }
        }

        private void WriteLoadLoc(int index)
        {
            if (PeekContext().GetLocalType(index).MaybeRef)
            {
                _gen.WriteLoadLoc((byte)index);
            }
            else
            {
                _gen.WriteLoadLocNoRef((byte)index);
            }
        }

        public void IncrementMaxStack()
        {
            // TODO: max stack counting for script safety
            _maxStack = -1;
        }

        private void Error(ScriptErrorCode code, string header, string message, ScriptToken token, ScriptErrorSeverity type)
        {
            Error(code, header, message, token.Pos, type);
        }

        private void Error(ScriptErrorCode code, string header, string message, int pos, ScriptErrorSeverity type)
        {
            ErrorHandler?.Invoke(this, new ScriptCompilerErrorEventArgs(code, header, message, pos, type));
        }

        public void SetTree(ScriptNode tree)
        {
            _tree = tree;
        }

        public ScriptCodeGenerator(SymbolTable symbolTable)
        {
            _locals = new List<Symbol>();
            _inVars = new Dictionary<string, ScriptInVarDefinition>();
            _symbolTable = symbolTable;
            _contextStack = new List<Context>();
            _tempVarIndexes = new Stack<int>();
            _loopCounters = new Stack<int>();
            _maxStack = 0;
            _gen = null;
            _tree = null;
        }
    }
}
