using System;

namespace DaveTheMonitor.ScriptSandbox
{
    internal sealed class ScriptAnalyzeEventArgs : EventArgs
    {
        public string[] AnalyzerErrors { get; private set; }
        public int Milliseconds { get; private set; }

        public ScriptAnalyzeEventArgs(string[] analyzerErrors, int milliseconds)
        {
            AnalyzerErrors = analyzerErrors;
            Milliseconds = milliseconds;
        }
    }
}
