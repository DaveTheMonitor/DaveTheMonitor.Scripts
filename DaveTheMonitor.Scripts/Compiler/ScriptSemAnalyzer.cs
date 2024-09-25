using DaveTheMonitor.Scripts.Compiler.Nodes;
using System;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class ScriptSemAnalyzer
    {
        private sealed class Context
        {
            public ContextType Type { get; set; }
            public Dictionary<Symbol, ScriptType> Symbols { get; set; }

            public Context(ContextType type)
            {
                Type = type;
                Symbols = new Dictionary<Symbol, ScriptType>();
            }
        }

        private enum ContextType : byte
        {
            Global,
            Loop,
            Condition
        }

        public event ScriptCompilerErrorEventHandler ErrorHandler;

        private static readonly int _void = 0;
        private static readonly int _scriptVar = 1;
        private static readonly int _double = 2;
        private static readonly int _long = 3;
        private static readonly int _bool = 4;
        private static readonly int _string = 5;
        private static readonly int _object = 6;
        private static readonly int[,] _addLookup =
        {
            // row = left, column = right, void = invalid
            //              | void      | scriptvar | double    | long      | bool      | string    | object     |
            /*      void */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /* scriptvar */ { _void     , _scriptVar, _scriptVar, _scriptVar, _scriptVar, _string   , _scriptVar },
            /*    double */ { _void     , _scriptVar, _double   , _double   , _void     , _string   , _void      },
            /*      long */ { _void     , _scriptVar, _double   , _long     , _void     , _string   , _void      },
            /*      bool */ { _void     , _void     , _void     , _void     , _void     , _string   , _void      },
            /*    string */ { _void     , _string   , _string   , _string   , _string   , _string   , _string    },
            /*    object */ { _void     , _void     , _void     , _void     , _void     , _string   , _void      },
        };
        private static readonly int[,] _arithmeticLookup =
        {
            // row = left, column = right, void = invalid
            //              | void      | scriptvar | double    | int       | bool      | string    | object     |
            /*      void */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /* scriptvar */ { _void     , _scriptVar, _scriptVar, _scriptVar, _void     , _void     , _void      },
            /*    double */ { _void     , _scriptVar, _double   , _double   , _void     , _void     , _void      },
            /*       int */ { _void     , _scriptVar, _double   , _long      , _void     , _void     , _void      },
            /*      bool */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /*    string */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /*    object */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
        };
        private static readonly int[,] _compareLookup =
        {
            // row = left, column = right, void = invalid
            //              | void      | scriptvar | double    | int       | bool      | string    | object     |
            /*      void */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /* scriptvar */ { _void     , _bool     , _bool     , _bool     , _void     , _void     , _void      },
            /*    double */ { _void     , _bool     , _bool     , _bool     , _void     , _void     , _void      },
            /*       int */ { _void     , _bool     , _bool     , _bool     , _void     , _void     , _void      },
            /*      bool */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /*    string */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
            /*    object */ { _void     , _void     , _void     , _void     , _void     , _void     , _void      },
        };

        private ScriptNode _tree;
        private SymbolTable _symbolTable;
        private Stack<Context> _contextStack;
        private List<string> _usings;
        private List<Symbol> _inVars;
        private List<ScriptMember> _memberResultList;
        private List<ScriptType> _typeResultList;
        private bool _usingValid;
        private bool _inValid;
        private bool _errored;

        public void Analyze(IEnumerable<string> usings)
        {
            _symbolTable.Clear();
            _contextStack.Clear();
            PushContext(ContextType.Global);
            _usings.Clear();
            _usings.Add(ScriptType.ScriptsNamespace);
            if (usings != null)
            {
                _usings.AddRange(usings);
            }
            _inVars.Clear();
            _usingValid = true;
            _inValid = true;

            foreach (StatementNode node in _tree.Children)
            {
                Analyze(node);
            }

            _contextStack.Clear();
            _usings.Clear();
            _inVars.Clear();
        }

        private void Analyze(StatementNode node)
        {
            if (node is UsingStatementNode usingNode)
            {
                Analyze(usingNode);
                return;
            }
            else if (node is InStatementNode inNode)
            {
                Analyze(inNode);
                return;
            }
            else
            {
                _usingValid = false;
                _inValid = false;
            }

            if (node is VarAssignmentStatementNode varNode)
            {
                Analyze(varNode);
            }
            else if (node is VarDeclarationStatementNode varDecNode)
            {
                Analyze(varDecNode);
            }
            else if (node is IfStatementNode ifNode)
            {
                Analyze(ifNode);
            }
            else if (node is WhileStatementNode whileNode)
            {
                Analyze(whileNode);
            }
            else if (node is ForStatementNode forNode)
            {
                Analyze(forNode);
            }
            else if (node is ForeachStatementNode foreachNode)
            {
                Analyze(foreachNode);
            }
            else if (node is LoopStatementNode loopNode)
            {
                Analyze(loopNode);
            }
            else if (node is BreakStatementNode breakNode)
            {
                Analyze(breakNode);
            }
            else if (node is ContinueStatementNode continueNode)
            {
                Analyze(continueNode);
            }
            else if (node is ReturnStatementNode returnNode)
            {
                Analyze(returnNode);
            }
            else if (node is ExitStatementNode exitNode)
            {
                Analyze(exitNode);
            }
            else if (node is FunctionStatementNode functionNode)
            {
                Analyze(functionNode);
            }
            else if (node is MemberStatementNode memberNode)
            {
                Analyze(memberNode);
            }
            else
            {
                throw new Exception("Invalid node in semantic analysis " + node.GetType().ToString());
            }
        }

        private void Analyze(UsingStatementNode node)
        {
            if (!_usingValid)
            {
                Error(ScriptErrorCode.S_InvalidUsing, "Invalid Using", "Using must be at top of file above In.", node.Start, ScriptErrorSeverity.Error);
            }
            foreach (ScriptToken token in node.Identifiers)
            {
                _usings.Add(token.Lexeme);
            }
        }

        private void Analyze(InStatementNode node)
        {
            if (!_inValid)
            {
                Error(ScriptErrorCode.S_InvalidInVar, "Invalid InVar", "In must be at top of file below Using.", node.Start, ScriptErrorSeverity.Error);
            }
            _usingValid = false;

            for (int i = 0; i < node.Identifiers.Length; i++)
            {
                ref InStatementNode.InIdentifier identifier = ref node.Identifiers[i];
                foreach (Symbol s in _inVars)
                {
                    if (s.Identifier == identifier.Identifier.Lexeme)
                    {
                        Error(ScriptErrorCode.S_DuplicateInVar, "Duplicate InVar", $"Duplicate declaration for InVar {s.Identifier}", identifier.Identifier.Pos, ScriptErrorSeverity.Error);
                        return;
                    }
                }

                ScriptType type;
                if (identifier.TypeIdentifier.HasValue)
                {
                    _typeResultList.Clear();
                    GetScriptTypes(identifier.TypeIdentifier.Value.Lexeme, _typeResultList);
                    if (_typeResultList.Count == 0)
                    {
                        Error(ScriptErrorCode.S_InvalidType, "Invalid Type", $"Type {identifier.TypeIdentifier.Value.Lexeme} could not be found.", identifier.TypeIdentifier.Value, ScriptErrorSeverity.Error);
                        type = ScriptType.GetType(typeof(ScriptVar));
                    }
                    else if (_typeResultList.Count > 1)
                    {
                        Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for type {identifier.TypeIdentifier.Value.Lexeme}", identifier.TypeIdentifier.Value, ScriptErrorSeverity.Error);
                        type = ScriptType.GetType(typeof(ScriptVar));
                    }
                    else if (!IsValidType(_typeResultList[0]))
                    {
                        Error(ScriptErrorCode.S_InvalidType, "Invalid Type", $"Type {_typeResultList[0].Name} is not valid", identifier.TypeIdentifier.Value, ScriptErrorSeverity.Error);
                        type = ScriptType.GetType(typeof(ScriptVar));
                    }
                    else
                    {
                        type = _typeResultList[0];
                    }
                }
                else
                {
                    type = ScriptType.GetType(typeof(ScriptVar));
                }

                identifier.Type = type;
                Symbol symbol = AddSymbol(identifier.Identifier);
                SetSymbolType(identifier.Identifier, type);
                if (_inVars.Contains(symbol))
                {
                    Error(ScriptErrorCode.S_DuplicateInVar, "Duplicate InVar", $"Duplicate declaration for InVar {symbol.Identifier}", identifier.Identifier, ScriptErrorSeverity.Error);
                }
                _inVars.Add(symbol);
            }
        }

        private void Analyze(VarAssignmentStatementNode node)
        {
            if (!TryGetSymbol(node.Identifier, out Symbol symbol))
            {
                symbol = new Symbol(node.Identifier.Lexeme);
            }
            if (_inVars.Contains(symbol))
            {
                Error(ScriptErrorCode.S_ConstAssignment, "InVar Assignment", "Cannot assign value to InVar after declaration", node.Identifier.Pos, ScriptErrorSeverity.Error);
                return;
            }
            bool declared = IsSymbolDeclared(symbol);

            if (node.Operator.Lexeme == "++" || node.Operator.Lexeme == "--")
            {
                if (!declared)
                {
                    Error(ScriptErrorCode.S_NoVarDecl, "Variable Use Before Declaration", $"Cannot use operator {node.Operator.Lexeme} on variable {node.Identifier.Lexeme} before declaration", node.Operator, ScriptErrorSeverity.Error);
                    AddSymbol(symbol);
                }

                ScriptType type = GetSymbolType(symbol);
                if (type != ScriptType.Long && type != ScriptType.Double && type != ScriptType.ScriptVar)
                {
                    Error(ScriptErrorCode.S_InvalidOperand, "Invalid Operand", $"Invalid operand {type.Name} for operator {node.Operator.Lexeme}", node.Identifier, ScriptErrorSeverity.Error);
                    node.OrigType = null;
                }
                else
                {
                    node.OrigType = type;
                }
            }
            else
            {
                if ((node.Operator.Lexeme == "+=" || node.Operator.Lexeme == "-=") && !declared)
                {
                    Error(ScriptErrorCode.S_NoVarDecl, "Variable Use Before Declaration", $"Cannot use operator {node.Operator.Lexeme} before variable is declared", node.Operator, ScriptErrorSeverity.Error);
                }

                ScriptType type = declared ? GetSymbolType(symbol) : null;
                node.OrigType = type;

                Analyze(node.Expression);
                ScriptType assignment = node.Expression.ResultType;
                if (!_symbolTable.HasSymbol(symbol.Identifier))
                {
                    AddSymbol(symbol);
                }
                SetSymbolType(symbol, assignment);
            }
        }

        private void Analyze(VarDeclarationStatementNode node)
        {
            if (!TryGetSymbol(node.Identifier, out Symbol symbol))
            {
                symbol = new Symbol(node.Identifier.Lexeme);
            }
            if (_inVars.Contains(symbol))
            {
                Error(ScriptErrorCode.S_ConstAssignment, "InVar Assignment", "Cannot declare an InVar as a variable", node.Identifier.Pos, ScriptErrorSeverity.Error);
                return;
            }

            if (IsSymbolDeclared(symbol))
            {
                Error(ScriptErrorCode.S_InvalidDeclaration, "Invalid Declaration", "Cannot declare a variable that has already been declared.", node.Identifier.Pos, ScriptErrorSeverity.Error);
                return;
            }

            if (!_symbolTable.HasSymbol(symbol.Identifier))
            {
                AddSymbol(symbol);
            }

            SetSymbolType(symbol, ScriptType.ScriptVar);
        }

        private void Analyze(IfStatementNode node)
        {
            Analyze(node.Condition);
            if (node.Condition.ResultType.Id != _bool && node.Condition.ResultType.Id != _scriptVar)
            {
                Error(ScriptErrorCode.S_InvalidCondition, "Invalid Condition", "If condition must return bool", node.Condition.Start, ScriptErrorSeverity.Error);
            }
            PushContext(ContextType.Condition);
            foreach (StatementNode st in node.Body)
            {
                Analyze(st);
            }
            PopContext();
            if (node.Else != null)
            {
                PushContext(ContextType.Condition);
                foreach (StatementNode st in node.Else)
                {
                    Analyze(st);
                }
                PopContext();
            }
        }

        private void Analyze(WhileStatementNode node)
        {
            Analyze(node.Condition);
            if (node.Condition.ResultType.Id != _bool && node.Condition.ResultType.Id != _scriptVar)
            {
                Error(ScriptErrorCode.S_InvalidCondition, "Invalid Condition", "While condition must return bool", node.Condition.Start, ScriptErrorSeverity.Error);
            }
            PushContext(ContextType.Condition);
            PushContext(ContextType.Loop);
            foreach (StatementNode st in node.Body)
            {
                Analyze(st);
            }
            PopContext();
            PopContext();
        }

        private void Analyze(ForStatementNode node)
        {
            PushContext(ContextType.Condition);
            Analyze(node.Initializer);
            Analyze(node.Condition);
            if (node.Condition.ResultType.Id != _bool && node.Condition.ResultType.Id != _scriptVar)
            {
                Error(ScriptErrorCode.S_InvalidCondition, "Invalid Condition", "For condition must return bool", node.Condition.Start, ScriptErrorSeverity.Error);
            }
            Analyze(node.Iterator);
            PushContext(ContextType.Loop);
            foreach (StatementNode st in node.Body)
            {
                Analyze(st);
            }
            PopContext();
            PopContext();
        }

        private void Analyze(ForeachStatementNode node)
        {
            PushContext(ContextType.Condition);

            Symbol symbol;
            if (IsSymbolDeclared(node.ItemIdentifier))
            {
                Error(ScriptErrorCode.S_InvalidVar, "Invalid Variable", "Foreach item variable is already declared.", node.ItemIdentifier, ScriptErrorSeverity.Error);
                symbol = GetSymbol(node.ItemIdentifier);
            }
            else
            {
                symbol = AddSymbol(node.ItemIdentifier);
            }

            Analyze(node.Iterator);
            if (node.Iterator.ResultType == null || !node.Iterator.ResultType.CanIterate)
            {
                Error(ScriptErrorCode.S_InvalidIteration, "Invalid Iteration", $"Cannot iterate over {node.Iterator.ResultType?.Name ?? ScriptType.ScriptVar.Type.Name}", node.Iterator.Start, ScriptErrorSeverity.Error);
            }

            SetSymbolType(symbol, node.Iterator.ResultType.IteratorGetItem.ReturnType);

            PushContext(ContextType.Loop);
            foreach (StatementNode st in node.Body)
            {
                Analyze(st);
            }
            PopContext();

            PopContext();
        }

        private void Analyze(LoopStatementNode node)
        {
            Analyze(node.Count);
            if (node.Count.ResultType.Id != _long && node.Count.ResultType.Id != _scriptVar)
            {
                Error(ScriptErrorCode.S_InvalidLoopCount, "Invalid Loop Count", "Loop count must return int", node.Count.Start, ScriptErrorSeverity.Error);
            }
            node.LiteralCount = node.Count is LiteralExpressionNode;
            if (node.LiteralCount)
            {
                LiteralExpressionNode lit = (LiteralExpressionNode)node.Count;
                long v = (long)lit.Value;
                if (v <= 0)
                {
                    Error(ScriptErrorCode.S_InvalidLoopCount, "Invalid Loop Count", "Cannot loop less than once", node.Count.Start, ScriptErrorSeverity.Error);
                }
            }
            PushContext(ContextType.Loop);
            foreach (StatementNode st in node.Body)
            {
                Analyze(st);
            }
            PopContext();
        }

        private void Analyze(BreakStatementNode node)
        {
            if (!InLoop())
            {
                Error(ScriptErrorCode.S_InvalidJump, "Invalid Break", "No loop to break", node.Start, ScriptErrorSeverity.Error);
            }
        }

        private void Analyze(ContinueStatementNode node)
        {
            if (!InLoop())
            {
                Error(ScriptErrorCode.S_InvalidJump, "Invalid Continue", "No loop to continue", node.Start, ScriptErrorSeverity.Error);
            }
        }

        private void Analyze(ReturnStatementNode node)
        {
            if (node.Expression != null)
            {
                Analyze(node.Expression);
            }
        }

        private void Analyze(ExitStatementNode node)
        {
            // exit nodes are valid anywhere and do not have any children
        }

        private void Analyze(FunctionStatementNode node)
        {
            Error(ScriptErrorCode.S_UnsupportedStatement, "Invalid Function", "Function is not supported", node.Start, ScriptErrorSeverity.Error);
        }

        private void Analyze(MemberStatementNode node)
        {
            Analyze((IMemberNode)node);
        }

        private void Analyze(ExpressionNode expr)
        {
            if (expr is CastExpressionNode castExpr)
            {
                Analyze(castExpr);
            }
            else if (expr is IdentifierExpressionNode identifierExpr)
            {
                Analyze(identifierExpr);
            }
            else if (expr is LiteralExpressionNode litExpr)
            {
                Analyze(litExpr);
            }
            else if (expr is MemberExpressionNode memberExpr)
            {
                Analyze(memberExpr);
            }
            else if (expr is CtorExpressionNode ctorExpr)
            {
                Analyze(ctorExpr);
            }
            else if (expr is OperatorExpressionNode operatorExpr)
            {
                Analyze(operatorExpr);
            }
            else if (expr is UnaryOperatorExpressionNode unaryExpr)
            {
                Analyze(unaryExpr);
            }
            else
            {
                throw new Exception("Invalid node in semantic analysis " + expr.GetType().ToString());
            }
        }

        private void Analyze(CastExpressionNode expr)
        {
            Error(ScriptErrorCode.S_UnsupportedExpr, "Unsupported Expression", "Cast is not supported", expr.Start, ScriptErrorSeverity.Error);
        }

        private void Analyze(IdentifierExpressionNode expr)
        {
            if (!TryGetSymbol(expr.Identifier, out Symbol symbol) || !IsSymbolDeclared(symbol))
            {
                Error(ScriptErrorCode.S_NoVarDecl, "Variable Use Before Declaration", $"Use of variable {expr.Identifier.Lexeme} before declaration", expr.Identifier, ScriptErrorSeverity.Error);
                expr.ResultType = ScriptType.GetType(_scriptVar);
                return;
            }

            ScriptType type = GetSymbolType(expr.Identifier);
            AddSymbolUsage(expr.Identifier, expr, type);
            expr.ResultType = type;
        }

        private void Analyze(LiteralExpressionNode expr)
        {
            switch (expr.Literal.Type)
            {
                case ScriptTokenType.NullLiteral:
                {
                    expr.ResultType = ScriptType.ScriptVar;
                    expr.Value = null;
                    break;
                }
                case ScriptTokenType.FalseLiteral:
                case ScriptTokenType.TrueLiteral:
                {
                    expr.ResultType = ScriptType.Bool;
                    expr.Value = expr.Literal.Lexeme == "true";
                    break;
                }
                case ScriptTokenType.NumLiteral:
                {
                    bool d = expr.Literal.Lexeme.Contains('.');
                    expr.ResultType = d ? ScriptType.Double : ScriptType.Long;
                    string parseStr = expr.Literal.Lexeme.Replace("_", null);
                    if (d)
                    {
                        if (!double.TryParse(parseStr, out double v))
                        {
                            Error(ScriptErrorCode.S_OutOfRange, "Value Out of Range", "Number out of range", expr.Literal, ScriptErrorSeverity.Error);
                        }
                        expr.Value = v;
                    }
                    else
                    {
                        if (!long.TryParse(parseStr, out long v))
                        {
                            Error(ScriptErrorCode.S_OutOfRange, "Value Out of Range", "Int out of range", expr.Literal, ScriptErrorSeverity.Error);
                        }
                        expr.Value = v;
                    }
                    break;
                }
                case ScriptTokenType.StringLiteral:
                {
                    expr.ResultType = ScriptType.String;
                    expr.Value = expr.Literal.Lexeme.Substring(1, expr.Literal.Lexeme.Length - 2);
                    break;
                }
                default:
                {
                    throw new Exception("Invalid literal type");
                }
            }
        }

        private void Analyze(MemberExpressionNode expr)
        {
            expr.ResultType = Analyze((IMemberNode)expr);
            if (expr.Member != null)
            {
                if (expr.Member is ScriptMethod method && method.Method.ReturnType == typeof(void))
                {
                    Error(ScriptErrorCode.S_InvalidExpressionType, "Invalid Expression Type", $"{method.Name} cannot be used in an expression because it returns void.", expr.Start, ScriptErrorSeverity.Error);
                }
            }
        }

        private void Analyze(CtorExpressionNode expr)
        {
            _typeResultList.Clear();
            GetScriptTypes(expr.TypeIdentifier.Lexeme, _typeResultList);
            if (_typeResultList.Count == 0)
            {
                Error(ScriptErrorCode.S_InvalidType, "Invalid Type", $"Type {expr.TypeIdentifier.Lexeme} could not be found.", expr.TypeIdentifier, ScriptErrorSeverity.Error);
                expr.ResultType = ScriptType.ScriptVar;
                return;
            }
            else if (_typeResultList.Count > 1)
            {
                Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for type {expr.TypeIdentifier.Lexeme}", expr.TypeIdentifier, ScriptErrorSeverity.Error);
                expr.ResultType = ScriptType.ScriptVar;
                return;
            }

            ScriptType type = _typeResultList[0];
            if (type.Ctor == null)
            {
                Error(ScriptErrorCode.S_InvalidConstructor, "Invalid Constructor", $"Type {type.Name} does not have a constructor.", expr.TypeIdentifier, ScriptErrorSeverity.Error);
                expr.ResultType = ScriptType.ScriptVar;
                return;
            }

            expr.ResultType = type;
            expr.Ctor = type.Ctor;

            if (type.Ctor.Args.Length != expr.Args.Length)
            {
                Error(ScriptErrorCode.S_InvalidArgCount, "Invalid Arguments", $"{type.Name} constructor expected {type.Ctor.Args.Length} arguments, received {expr.Args.Length}", expr.TypeIdentifier, ScriptErrorSeverity.Error);
                return;
            }

            for (int i = 0; i < type.Ctor.Args.Length; i++)
            {
                ExpressionNode arg = expr.Args[i];
                Analyze(arg);

                ScriptType expected = type.Ctor.Args[i];
                if (expected == ScriptType.GetType(typeof(ScriptVar)))
                {
                    continue;
                }

                if (arg.ResultType != ScriptType.GetType(typeof(ScriptVar)) &&
                    !IsCompatible(expected, arg.ResultType))
                {
                    Error(ScriptErrorCode.S_InvalidArgType, "Argument Error", $"{type.Name} constructor argument {i} expected {type.Ctor.Args[i].Name}, received {arg.ResultType.Name}", arg.Start, ScriptErrorSeverity.Error);
                    return;
                }
            }
        }

        private void Analyze(OperatorExpressionNode expr)
        {
            Analyze(expr.Left);
            Analyze(expr.Right);
            int result = OperationResult(expr.Left.ResultType, expr.Operator, expr.Right.ResultType);
            if (result == 0)
            {
                Error(ScriptErrorCode.S_InvalidOperand, "Invalid Operands", $"Invalid operands for operator {expr.Operator.Lexeme}: {expr.Left.ResultType.Name}, {expr.Right.ResultType.Name}", expr.Start, ScriptErrorSeverity.Error);
                expr.ResultType = ScriptType.GetType(_scriptVar);
                return;
            }
            else if (result == -1)
            {
                // should never be thrown
                throw new Exception("Invalid operator");
            }
            expr.ResultType = ScriptType.GetType(result);
        }

        private int OperationResult(ScriptType left, ScriptToken op, ScriptType right)
        {
            return op.Lexeme switch
            {
                "+" => _addLookup[left.Id, right.Id],
                "-" => _arithmeticLookup[left.Id, right.Id],
                "*" => _arithmeticLookup[left.Id, right.Id],
                "/" => _arithmeticLookup[left.Id, right.Id],
                "%" => _arithmeticLookup[left.Id, right.Id],
                "<" => _compareLookup[left.Id, right.Id],
                "<=" => _compareLookup[left.Id, right.Id],
                ">" => _compareLookup[left.Id, right.Id],
                ">=" => _compareLookup[left.Id, right.Id],
                "==" => _bool,
                "!=" => _bool,
                "and" => left.Id == _bool && right.Id == _bool ? _bool : _void,
                "or" => left.Id == _bool && right.Id == _bool ? _bool : _void,
                "&&" => left.Id == _bool && right.Id == _bool ? _bool : _void,
                "||" => left.Id == _bool && right.Id == _bool ? _bool : _void,
                _ => -1,
            };
        }

        private void Analyze(UnaryOperatorExpressionNode expr)
        {
            Analyze(expr.Operand);
            ScriptType type = expr.Operand.ResultType;
            expr.ResultType = expr.Operand.ResultType;
            if ((expr.Operator.Lexeme == "+" || expr.Operator.Lexeme == "-") && type != ScriptType.ScriptVar && type != ScriptType.Double && type != ScriptType.Long)
            {
                Error(ScriptErrorCode.S_InvalidOperand, "Invalid Operands", $"Invalid operand for operator {expr.Operator.Lexeme}: {expr.Operand.ResultType.Name}", expr.Start, ScriptErrorSeverity.Error);
            }
            else if ((expr.Operator.Lexeme == "!" || expr.Operator.Lexeme == "not") && type != ScriptType.ScriptVar && type != ScriptType.Bool)
            {
                Error(ScriptErrorCode.S_InvalidOperand, "Invalid Operands", $"Invalid operand for operator {expr.Operator.Lexeme}: {expr.Operand.ResultType.Name}", expr.Start, ScriptErrorSeverity.Error);
            }
        }

        private ScriptType Analyze(IMemberNode node)
        {
            Symbol objSymbol = null;
            bool staticMember;
            if (node.ObjectIdentifier.HasValue)
            {
                if (!TryGetSymbol(node.ObjectIdentifier.Value, out objSymbol) || !IsSymbolDeclared(objSymbol))
                {
                    Error(ScriptErrorCode.S_NoVarDecl, "Variable Use Before Declaration", $"Use of variable identifier {node.ObjectIdentifier.Value.Lexeme} before declaration", node.ObjectIdentifier.Value, ScriptErrorSeverity.Error);
                }
                staticMember = false;
            }
            else
            {
                staticMember = true;
            }
            ScriptType objType = staticMember ? null : objSymbol != null ? GetSymbolType(objSymbol) : ScriptType.ScriptVar;

            ScriptMethod method = null;
            ScriptProperty prop = null;
            ScriptConst constant = null;
            int found = 0;
            string name = node.Identifier.Lexeme;

            if (staticMember)
            {
                GetStaticMethods(name, _memberResultList);
                if (_memberResultList.Count > 0)
                {
                    if (_memberResultList.Count > 1)
                    {
                        Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for static method {name}", node.Identifier, ScriptErrorSeverity.Error);
                        return ScriptType.ScriptVar;
                    }
                    else
                    {
                        method = (ScriptMethod)_memberResultList[0];
                    }
                    found++;
                }
                GetStaticProperties(name, _memberResultList);
                if (_memberResultList.Count > 0)
                {
                    if (_memberResultList.Count > 1)
                    {
                        Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for static property {name}", node.Identifier, ScriptErrorSeverity.Error);
                        return ScriptType.ScriptVar;
                    }
                    else
                    {
                        prop = (ScriptProperty)_memberResultList[0];
                    }
                    found++;
                }
                GetConsts(name, _memberResultList);
                if (_memberResultList.Count > 0)
                {
                    if (_memberResultList.Count > 1)
                    {
                        Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for const {name}", node.Identifier, ScriptErrorSeverity.Error);
                        return ScriptType.ScriptVar;
                    }
                    else
                    {
                        constant = (ScriptConst)_memberResultList[0];
                    }
                    found++;
                }
            }
            else if (objType != ScriptType.GetType(typeof(ScriptVar)))
            {
                method = objType.GetMethod(name);
                if (method != null)
                {
                    found++;
                }
                prop = objType.GetProperty(name);
                if (prop != null)
                {
                    found++;
                }
            }

            if (objType != ScriptType.GetType(typeof(ScriptVar)) && found == 0)
            {
                Error(ScriptErrorCode.S_InvalidMember, "Invalid Member", $"Cannot find {(staticMember ? "static " : $"{GetSymbolType(objSymbol).Name} ")}method or property with name {name}", node.Identifier, ScriptErrorSeverity.Error);
            }
            else if (found > 1)
            {
                Error(ScriptErrorCode.S_AmbiguousMatch, "Ambiguous Match", $"Ambiguous match for {(staticMember ? "static " : $"{GetSymbolType(objSymbol).Name} ")} method or property {name}", node.Identifier, ScriptErrorSeverity.Error);
            }
            else if (method != null || objType?.Id == _scriptVar)
            {
                if (method != null)
                {
                    node.Member = method;
                    if (method.Args.Length != (node.Args?.Length ?? 0))
                    {
                        Error(ScriptErrorCode.S_InvalidArgCount, "Invalid Arguments", $"{method.Name} expected {method.Args.Length} arguments, received {(node.Args != null ? node.Args.Length : 0)}", node.Identifier, ScriptErrorSeverity.Error);
                    }
                }
                
                if (node.Args != null)
                {
                    for (int i = 0; i < node.Args.Length; i++)
                    {
                        ExpressionNode arg = node.Args[i];
                        Analyze(arg);
                        if (method == null || method.Args.Length <= i || method.Args[i] == ScriptType.GetType(typeof(ScriptVar)))
                        {
                            continue;
                        }

                        ScriptType expected = method.Args[i];
                        if (arg.ResultType != ScriptType.GetType(typeof(ScriptVar)) &&
                            expected != ScriptType.GetType(typeof(ScriptVar)) &&
                            !IsCompatible(expected, arg.ResultType))
                        {
                            Error(ScriptErrorCode.S_InvalidArgType, "Argument Error", $"{method.Name} argument {i} expected {method.Args[i].Name}, received {arg.ResultType.Name}", arg.Start, ScriptErrorSeverity.Error);
                        }
                    }
                }

                if (method != null)
                {
                    return method.ReturnType;
                }
            }
            else if (prop != null)
            {
                node.Member = prop;
                return prop.PropertyType;
            }
            else if (constant != null)
            {
                node.Member = constant;
                return constant.Type;
            }

            node.Member = null;
            return ScriptType.ScriptVar;
        }

        public bool InLoop()
        {
            foreach (Context context in _contextStack)
            {
                if (context.Type == ContextType.Loop)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsValidType(ScriptType type)
        {
            return type != ScriptType.Void;
        }

        private bool IsCompatible(ScriptType expected, ScriptType received)
        {
            return expected == received || (expected == ScriptType.Double && received == ScriptType.Long);
        }

        private void GetScriptTypes(string name, List<ScriptType> list)
        {
            int dot = name.LastIndexOf('.');
            string @namespace;
            string typeName;
            if (dot != -1)
            {
                @namespace = name.Substring(0, dot);
                typeName = name.Substring(dot + 1);
                ScriptType type = ScriptType.GetType(@namespace, typeName);
                if (type != null)
                {
                    list.Add(type);
                }
            }
            else
            {
                List<ScriptType> types = ScriptType.GetTypes(_usings, name);
                foreach (ScriptType type in types)
                {
                    list.Add(type);
                }
            }
        }

        private void GetStaticMethods(string name, List<ScriptMember> list)
        {
            list.Clear();
            int dot = name.LastIndexOf('.');
            if (dot != -1)
            {
                ScriptMethod m = ScriptMethod.GetStaticMethod(name.Substring(0, dot), name.Substring(dot + 1));
                if (m != null)
                {
                    list.Add(m);
                }
            }
            else
            {
                List<ScriptMethod> methods = ScriptMethod.GetStaticMethods(_usings, name);
                foreach (ScriptMethod m in methods)
                {
                    list.Add(m);
                }
            }
        }

        private void GetStaticProperties(string name, List<ScriptMember> list)
        {
            list.Clear();
            int dot = name.LastIndexOf('.');
            if (dot != -1)
            {
                ScriptProperty p = ScriptProperty.GetStaticProperty(name.Substring(0, dot), name.Substring(dot + 1));
                if (p != null)
                {
                    list.Add(p);
                }
            }
            else
            {
                List<ScriptProperty> properties = ScriptProperty.GetStaticProperties(_usings, name);
                foreach (ScriptProperty p in properties)
                {
                    list.Add(p);
                }
            }
        }

        private void GetConsts(string name, List<ScriptMember> list)
        {
            list.Clear();
            int dot = name.LastIndexOf('.');
            if (dot != -1)
            {
                ScriptConst c = ScriptConst.GetConst(name.Substring(0, dot), name.Substring(dot + 1));
                if (c != null)
                {
                    list.Add(c);
                }
            }
            else
            {
                List<ScriptConst> consts = ScriptConst.GetConsts(_usings, name);
                foreach (ScriptConst c in consts)
                {
                    list.Add(c);
                }
            }
        }

        private ScriptType GetSymbolType(ScriptToken identifier)
        {
            return GetSymbolType(_symbolTable.GetSymbol(identifier.Lexeme));
        }

        private ScriptType GetSymbolType(Symbol symbol)
        {
            return _contextStack.Peek().Symbols[symbol];
        }

        private void SetSymbolType(ScriptToken symbolIdentifier, ScriptType type)
        {
            SetSymbolType(GetSymbol(symbolIdentifier), type);
        }

        private void SetSymbolType(Symbol symbol, ScriptType type)
        {
            _contextStack.Peek().Symbols[symbol] = type;
        }

        private Symbol GetSymbol(ScriptToken identifier)
        {
            return _symbolTable.GetSymbol(identifier.Lexeme);
        }

        private bool TryGetSymbol(ScriptToken identifier, out Symbol symbol)
        {
            //foreach (Symbol s in _contextStack.Peek().Symbols.Keys)
            //{
            //    if (s.Identifier == identifier.Lexeme)
            //    {
            //        symbol = s;
            //        return true;
            //    }
            //}
            //symbol = null;
            //return false;
            return _symbolTable.TryGetValue(identifier.Lexeme, out symbol);
        }

        private Symbol AddSymbol(ScriptToken identifier)
        {
            Symbol symbol = _symbolTable.AddSymbol(identifier.Lexeme);
            return symbol;
        }

        private void AddSymbol(Symbol symbol)
        {
            _symbolTable.AddSymbol(symbol);
        }

        private void AddSymbolUsage(ScriptToken identifier, ExpressionNode node, ScriptType type)
        {
            _symbolTable.AddSymbolUsage(identifier.Lexeme, node, type);
        }

        private bool IsSymbolDeclared(Symbol symbol)
        {
            return _contextStack.Peek().Symbols.ContainsKey(symbol);
        }

        private bool IsSymbolDeclared(ScriptToken identifier)
        {
            if (TryGetSymbol(identifier, out Symbol symbol))
            {
                return IsSymbolDeclared(symbol);
            }
            else
            {
                return false;
            }
        }

        private void PushContext(ContextType type)
        {
            Context context = new Context(type);
            if (_contextStack.Count > 0)
            {
                Context currentContext = _contextStack.Peek();
                foreach (KeyValuePair<Symbol, ScriptType> pair in currentContext.Symbols)
                {
                    context.Symbols.Add(pair.Key, pair.Value);
                }
            }
            _contextStack.Push(context);
        }

        private void PopContext()
        {
            Context context = _contextStack.Pop();
            if (_contextStack.Count == 0)
            {
                return;
            }

            Context currentContext = _contextStack.Peek();
            if (context.Type == ContextType.Condition)
            {
                foreach (KeyValuePair<Symbol, ScriptType> pair in context.Symbols)
                {
                    if (!currentContext.Symbols.TryGetValue(pair.Key, out ScriptType prev))
                    {
                        continue;
                    }

                    // Because conditions aren't always executed, there's no
                    // guaranteeing that the symbol type is the new or old
                    // type, meaning we can't assume the type for any future
                    // usages before setting it to something else.
                    if (prev != pair.Value)
                    {
                        currentContext.Symbols[pair.Key] = ScriptType.GetType(typeof(ScriptVar));
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<Symbol, ScriptType> pair in context.Symbols)
                {
                    // Loops are always executed, so we can assume that
                    // the symbol type is the new type until it's set again
                    // or the context is a condition.

                    if (!currentContext.Symbols.ContainsKey(pair.Key))
                    {
                        continue;
                    }

                    currentContext.Symbols[pair.Key] = pair.Value;
                }
            }
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

        public ScriptSemAnalyzer(SymbolTable symbolTable)
        {
            _tree = null;
            _symbolTable = symbolTable;
            _contextStack = new Stack<Context>();
            _memberResultList = new List<ScriptMember>();
            _typeResultList = new List<ScriptType>();
            _usings = new List<string>();
            _inVars = new List<Symbol>();
        }
    }
}
