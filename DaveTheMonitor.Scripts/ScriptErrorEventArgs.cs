using System;

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptErrorEventArgs : EventArgs
    {
        public ScriptErrorCode Code { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public ScriptErrorSeverity Severity { get; private set; }

        public ScriptErrorEventArgs(ScriptErrorCode code, string header, string message, ScriptErrorSeverity type)
        {
            Code = code;
            Header = header;
            Message = message;
            Severity = type;
        }
    }
}
