using System;

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptPrintEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public ScriptPrintEventArgs(string message)
        {
            Message = message;
        }
    }
}
