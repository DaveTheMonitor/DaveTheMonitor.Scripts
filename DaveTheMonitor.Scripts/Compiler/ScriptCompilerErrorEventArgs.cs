using System;

namespace DaveTheMonitor.Scripts.Compiler
{
    public sealed class ScriptCompilerErrorEventArgs : EventArgs
    {
        public ScriptErrorCode Code { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public int Pos { get; private set; }
        public ScriptErrorSeverity Severity { get; private set; }

        public ScriptCompilerErrorEventArgs(ScriptErrorCode code, string header, string message, int pos, ScriptErrorSeverity type)
        {
            Code = code;
            Header = header;
            Message = message;
            Pos = pos;
            Severity = type;
        }
    }
}
