using DaveTheMonitor.Scripts.Compiler.Nodes;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    public sealed class ScriptCompiler : IScriptCompiler
    {
        public event ScriptCompilerErrorEventHandler ErrorHandler;
        private ScriptTokenizer _tokenizer;
        private ScriptParser _parser;
        private ScriptSemAnalyzer _semAnalyzer;
        private ScriptCodeGenerator _generator;
        private SymbolTable _symbolTable;
        private string _src;
        private bool _errored;

        public Script Compile(string name, ScriptRuntimeType type, CompilerOptimization optimization)
        {
            return Compile(name, type, optimization, null);
        }

        public Script Compile(string name, ScriptRuntimeType type, CompilerOptimization optimization, IEnumerable<string> usings)
        {
            _errored = false;

            _tokenizer.SetSrc(_src);
            ScriptToken[] tokens = _tokenizer.Tokenize();

            if (_errored)
            {
                return null;
            }

            _parser.SetTokens(StripComments(tokens));
            ScriptNode tree = _parser.Parse();

            if (_errored)
            {
                return null;
            }

            _semAnalyzer.SetTree(tree);
            _semAnalyzer.Analyze(usings);

            if (_errored)
            {
                return null;
            }
            
            _generator.SetTree(tree);
            Script script = _generator.Compile(name, type);

            if (_errored)
            {
                return null;
            }

            return script;
        }

        public bool Analyze()
        {
            return Analyze(null);
        }

        public bool Analyze(IEnumerable<string> usings)
        {
            _errored = false;

            _tokenizer.SetSrc(_src);
            ScriptToken[] tokens = _tokenizer.Tokenize();

            if (_errored)
            {
                return false;
            }

            _parser.SetTokens(StripComments(tokens));
            ScriptNode tree = _parser.Parse();

            if (_errored)
            {
                return false;
            }

            _semAnalyzer.SetTree(tree);
            _semAnalyzer.Analyze(usings);

            return _errored;
        }

        private void HandleError(object sender, ScriptCompilerErrorEventArgs e)
        {
            _errored = true;
            ErrorHandler?.Invoke(this, e);
        }

        private ScriptToken[] StripComments(ScriptToken[] tokens)
        {
            List<ScriptToken> list = new List<ScriptToken>();
            foreach (ScriptToken token in tokens)
            {
                if (token.Type != ScriptTokenType.Comment)
                {
                    list.Add(token);
                }
            }
            return list.ToArray();
        }

        public void SetSrc(string src)
        {
            _src = src;
        }

        public ScriptCompiler()
        {
            _symbolTable = new SymbolTable();
            _tokenizer = new ScriptTokenizer();
            _tokenizer.ErrorHandler += HandleError;
            _parser = new ScriptParser();
            _parser.ErrorHandler += HandleError;
            _semAnalyzer = new ScriptSemAnalyzer(_symbolTable);
            _semAnalyzer.ErrorHandler += HandleError;
            _generator = new ScriptCodeGenerator(_symbolTable);
            _generator.ErrorHandler += HandleError;
        }
    }
}
