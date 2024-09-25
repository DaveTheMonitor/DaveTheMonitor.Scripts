using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    public interface IScriptCompiler
    {
        event ScriptCompilerErrorEventHandler ErrorHandler;
        Script Compile(string name, ScriptRuntimeType type, CompilerOptimization optimization);
        Script Compile(string name, ScriptRuntimeType type, CompilerOptimization optimization, IEnumerable<string> usings = null);
        bool Analyze();
        bool Analyze(IEnumerable<string> usings);
        void SetSrc(string src);
    }
}
