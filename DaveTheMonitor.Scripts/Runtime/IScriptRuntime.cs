using System;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts
{
    public interface IScriptRuntime : IDisposable
    {
        ScriptRuntimeType RuntimeType { get; }
        IScriptReference Reference { get; }
        ScriptVar ReturnedValue { get; }
        object ReturnedObject { get; }
        void Return(ScriptVar v);
        void Print(string value);
        void RunScript(Script script);
        void RunScript(Script script, ScriptInVar inVar);
        void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1);
        void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1, ScriptInVar inVar2);
        void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1, ScriptInVar inVar2, ScriptInVar inVar3);
        void RunScript(Script script, ReadOnlySpan<ScriptInVar> inVars);
        void RunScript(Script script, List<ScriptInVar> inVars);
        void RunScript(Script script, params ScriptInVar[] inVars);
        void Error(ScriptErrorCode code, string header, string message);
        void Warn(ScriptErrorCode code, string header, string message);
    }
}
