using DaveTheMonitor.Scripts.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace DaveTheMonitor.ScriptSandbox
{
    internal sealed class EditorAnalyzer
    {
        public event ScriptAnalyzeEventHandler AnalyzeHandler;
        private char[] _separators;
        private bool _shouldAnalyze;
        private ScriptCompiler _analyzer;
        private string _src;
        private string _analyzing;
        private string _prevSrc;
        private List<string> _analyzerErrors;
        private StringBuilder _messageBuilder;
        private Stopwatch _changeTimer;
        private Stopwatch _analysisTimer;
        private bool _errored;

        public void SetSrc(string src)
        {
            _src = src;
            _changeTimer.Restart();
        }

        private void Analyze()
        {
            string src = _src;
            // We don't analyze for a while if the previous analysis had no errors
            // to avoid flooding the console while typing.
            if (src != _prevSrc && (_changeTimer.ElapsedMilliseconds > 1000 || _errored))
            {
                _analysisTimer.Restart();
                _errored = false;
                _analyzing = _src.Replace("\r\n", "\n").Replace('\r', '\n');
                _analyzerErrors.Clear();
                if (src != null)
                {
                    try
                    {
                        _analyzer.SetSrc(_analyzing);
                        _analyzer.Analyze(null);
                    }
                    catch (Exception ex)
                    {
                        _analyzerErrors.Clear();
                        _analyzerErrors.Add($"Could not analyze due to exception:\n    {ex.Message}");
                    }
                }

                _analysisTimer.Stop();
                AnalyzeHandler?.Invoke(this, new ScriptAnalyzeEventArgs(_analyzerErrors.ToArray(), (int)_analysisTimer.ElapsedMilliseconds));
                _analyzerErrors.Clear();
                _analyzing = null;
                _prevSrc = src;
            }
        }

        public void StartAnalyzing()
        {
            _src = null;
            _prevSrc = null;
            _shouldAnalyze = true;
            while (_shouldAnalyze)
            {
                Analyze();
                Thread.Sleep(300);
            }
        }

        public void StopAnalyzing()
        {
            _shouldAnalyze = false;
        }

        public void PreventAnalyzing()
        {
            _src = null;
            _prevSrc = null;
        }

        private void HandleAnalyzerError(object sender, ScriptCompilerErrorEventArgs e)
        {
            _errored = true;
            string src = _analyzing;
            _messageBuilder.Clear();
            int line = 1;
            int c = 1;
            for (int i = 0; i < Math.Min(e.Pos, src.Length); i++)
            {
                c++;
                if (src[i] == '\n')
                {
                    line++;
                    c = 1;
                }
            }


            _messageBuilder.Append(e.Header);
            _messageBuilder.Append(":\n    ");
            _messageBuilder.Append(e.Message);
            _messageBuilder.Append(" at ");
            _messageBuilder.Append(line);
            _messageBuilder.Append(", ");
            _messageBuilder.Append(c);
            _analyzerErrors.Add(_messageBuilder.ToString());
        }

        public EditorAnalyzer()
        {
            _analyzer = new ScriptCompiler();
            _analyzer.ErrorHandler += HandleAnalyzerError;
            _analyzerErrors = new List<string>();
            _messageBuilder = new StringBuilder();
            _changeTimer = new Stopwatch();
            _analysisTimer = new Stopwatch();
            _separators = new char[] { '\n', '\u0009' };
            _src = null;
            _prevSrc = null;
        }
    }
}
