using DaveTheMonitor.Scripts;
using DaveTheMonitor.Scripts.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DaveTheMonitor.ScriptSandbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double _zoomInFactor = 110d / 100d;
        private const double _zoomOutFactor = 100d / 110d;
        private bool OpsVisible => _opOutput.Visibility == Visibility.Visible;
        private int _prevLines;
        private StringBuilder _lineBuilder;
        private ScriptCompiler _compiler;
        private ScriptCILCompiler _cilCompiler;
        private EditorAnalyzer _analyzer;
        private List<string> _output;
        private ScriptRuntime _runtime;
        private Script _script;
        private Thread _analyzerThread;
        private bool _analyzing;
        private double _editorZoom;
        private IndentType _indentType;
        private int _indentWidth;
        private string _indentString;
        private bool _cilOutput;
        private double _opWidth;
        private string _prevSrc;
        private bool _compiledCil;
        private Stopwatch _scriptTimer;

        public MainWindow()
        {
            ScriptType.RegisterTypes(new Assembly[] { Assembly.GetExecutingAssembly() });
            InitializeComponent();
            _prevLines = 0;
            _lineBuilder = new StringBuilder();
            _lineBox.Text = "1";
            _compiler = new ScriptCompiler();
            _analyzer = new EditorAnalyzer();
            _analyzer.AnalyzeHandler += HandleAnalyze;
            _compiler.ErrorHandler += HandleCompilerError;
            _output = new List<string>();
            _runtime = new ScriptRuntime(1024, 1024, 1024);
            _runtime.ErrorHandler += HandleRuntimeError;
            _runtime.PrintHandler += HandlePrint;
            _analyzerThread = null;
            _analyzing = false;
            _editorZoom = 1;
            _indentType = IndentType.Space;
            _indentWidth = 4;
            _indentString = "    ";
            _opWidth = 250;
            _scriptTimer = new Stopwatch();
            PreviewMouseWheel += HandleMouseWheel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void CompileAndRun()
        {
#if !DEBUG
            try
#endif
            {
                ClearOutput();
                DispatchLog("Compiling script...");

                _opOutput.Clear();

                _prevSrc = _codeInput.Text;
                _compiler.SetSrc(_codeInput.Text);
                Script script = _compiler.Compile("", ScriptRuntimeType.Mod, CompilerOptimization.Basic);

                if (script != null)
                {
                    DispatchLog("Script compiled successfully.");
                    if (_cilOutput)
                    {
                        _compiledCil = true;
                        DispatchLog("Compiling CIL...");
                        if (_cilCompiler == null)
                        {
                            _cilCompiler = new ScriptCILCompiler();
                            _cilCompiler.OutputCILString = true;
                        }

                        _cilCompiler.Compile(script);
                        DispatchLog("CIL Compiled Successfully");
                        _opOutput.Text = _cilCompiler.GetCILOutputString();
                    }
                    else
                    {
                        _compiledCil = false;
                        _opOutput.Text = script.GetBytecodeString(true);
                    }

                    _script = script;
                    _compileButton.IsEnabled = false;

                    DispatchLog("Script executing...");
                    Task.Run(StartScript);
                }
                else
                {
                    DispatchLog($"Script compilation failed.");
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                DispatchLog(ex.ToString());
            }
#endif
        }

        private void StartScript()
        {
            _scriptTimer.Restart();
            _runtime.RunScript(_script);
            _scriptTimer.Stop();
            DispatchLog($"Script execution finished in {((_scriptTimer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000):0.0000}ms.{Environment.NewLine}Returned {_runtime.ReturnedValue}");
            Dispatcher.Invoke(() => _compileButton.IsEnabled = true);
        }

        private void DispatchLog(string message)
        {
            Dispatcher.Invoke(() => Log(message));
        }

        private void Log(string message)
        {
            _output.Add(message);
            _outputBox.Text += message + "\n";
        }

        private void DispatchLogAndClear(IEnumerable<string> messages)
        {
            Dispatcher.Invoke(() => LogAndClear(messages));
        }

        private void DispatchLogAndClear(string header, IEnumerable<string> messages)
        {
            Dispatcher.Invoke(() => LogAndClear(header, messages));
        }

        private void LogAndClear(IEnumerable<string> messages)
        {
            ClearOutput();
            StringBuilder builder = new StringBuilder();
            foreach (string str in messages)
            {
                _output.Add(str);
                builder.AppendLine(str);
            }
            _outputBox.Text = builder.ToString();
        }

        private void LogAndClear(string header, IEnumerable<string> messages)
        {
            ClearOutput();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(header);
            foreach (string s in messages)
            {
                _output.Add(s);
                builder.AppendLine(s);
            }
            _outputBox.Text = builder.ToString();
        }

        private void ClearOutput()
        {
            _output.Clear();
            _outputBox.Clear();
        }

        private void HandleCompilerError(object sender, ScriptCompilerErrorEventArgs e)
        {
            Log(e.Header + $":{Environment.NewLine}    " + e.Message);
        }

        private void HandleRuntimeError(object sender, ScriptErrorEventArgs e)
        {
            DispatchLog(e.Header + $":{Environment.NewLine}    " + e.Message);
            Dispatcher.Invoke(() => _compileButton.IsEnabled = true);
        }

        private void HandleAnalyze(object sender, ScriptAnalyzeEventArgs e)
        {
            if (e.Milliseconds > 150)
            {
                DispatchLogAndClear($"WARNING: Analysis is slow ({e.Milliseconds}ms)", e.AnalyzerErrors);
            }
            else
            {
                DispatchLogAndClear(e.AnalyzerErrors);
            }
        }

        private void HandlePrint(object sender, ScriptPrintEventArgs e)
        {
            DispatchLog(e.Message);
        }

        private void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && _codeInput.IsFocused)
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }
                e.Handled = true;
            }
        }

        private void HideOps()
        {
            _opWidth = _opOutput.Width;
            _opOutput.Width = 0;
            _opSeparator.Width = 0;
            _opOutput.Visibility = Visibility.Hidden;
            _opSeparator.Visibility = Visibility.Hidden;
            _toggleOpButton.Content = GetOpsButtonString();
        }

        private void ShowOps()
        {
            _opOutput.Width = _opWidth;
            _opSeparator.Width = 2;
            _opOutput.Visibility = Visibility.Visible;
            _opSeparator.Visibility = Visibility.Visible;
            _toggleOpButton.Content = GetOpsButtonString();
        }

        private string GetOpsButtonString()
        {
            if (_cilOutput)
            {
                return OpsVisible ? "Hide CIL" : "Show CIL";
            }
            else
            {
                return OpsVisible ? "Hide Ops" : "Show Ops";
            }
        }

        private void ZoomOut()
        {
            double zoom = _editorZoom * _zoomOutFactor;
            if (zoom >= 0.25)
            {
                SetZoom(zoom);
            }
        }

        private void ZoomIn()
        {
            double zoom = _editorZoom * _zoomInFactor;
            if (zoom <= 4)
            {
                SetZoom(zoom);
            }
        }

        private void SetZoom(double zoom)
        {
            // Ensures you can't get a zoom like 1.0000000000000009%
            if (Math.Abs(1 - zoom) < 0.001)
            {
                zoom = 1;
            }
            _editorZoom = zoom;
            _currZoomBox.Text = zoom.ToString("P0");
            _codeInputScaleTransform.ScaleX = zoom;
            _codeInputScaleTransform.ScaleY = zoom;
            _lineBoxScaleTransform.ScaleX = zoom;
            _lineBoxScaleTransform.ScaleY = zoom;
            _lineColumn.Width = new GridLength(30 * zoom, _lineColumn.Width.GridUnitType);
        }

        private void Indent()
        {
            int start = _codeInput.SelectionStart;
            int startLine = _codeInput.GetLineIndexFromCharacterIndex(start);
            IndentSingleLine(start, startLine);
            return;

            if (_codeInput.SelectionLength > 0)
            {
                int length = _codeInput.SelectionLength;
                int endLine = _codeInput.GetLineIndexFromCharacterIndex(start + length);
                if (startLine != endLine)
                {
                    IndentMultiLine(start, length, startLine, endLine);
                    return;
                }
            }
            IndentSingleLine(start, startLine);
        }

        private void IndentSingleLine(int start, int startLine)
        {
            if (_indentString.Length == 1)
            {
                _codeInput.SelectedText = _indentString;
                _codeInput.SelectionLength = 0;
                _codeInput.CaretIndex = start + 1;
                return;
            }

            int lineStart = _codeInput.GetCharacterIndexFromLineIndex(startLine);
            int lineCaretIndex = start - lineStart;
            int targetLength = _indentString.Length - (lineCaretIndex % _indentString.Length);
            string insert = targetLength == _indentString.Length ? _indentString : _indentString.Substring(0, targetLength);

            _codeInput.SelectedText = insert;
            _codeInput.SelectionLength = 0;
            _codeInput.CaretIndex = start + insert.Length;
        }

        private void IndentMultiLine(int start, int length, int startLine, int endLine)
        {
            double veritcalScroll = _codeInputScroll.VerticalOffset;
            double horizontalScroll = _codeInputScroll.HorizontalOffset;
            int lines = endLine - startLine + 1;
            int lineCount = _codeInput.LineCount;
            int firstAdded = 0;
            int added = 0;

            StringBuilder builder = new StringBuilder(_codeInput.Text.Length + (_indentString.Length * lines));
            builder.Append(_codeInput.Text.AsSpan(0, _codeInput.GetCharacterIndexFromLineIndex(startLine)));
            for (int i = 0; i < lines; i++)
            {
                string original = _codeInput.GetLineText(startLine + i);
                int index = -1;
                for (int j = 0; j < original.Length; j++)
                {
                    if (j == original.Length || !char.IsWhiteSpace(original[j]))
                    {
                        index = j;
                        break;
                    }
                }

                int indentLength = index % _indentString.Length;
                ReadOnlySpan<char> indent;
                if (indentLength == 0)
                {
                    indentLength = _indentString.Length;
                    indent = _indentString;
                }
                else
                {
                    indent = _indentString.AsSpan(indentLength);
                }
                if (i == 0)
                {
                    firstAdded = indentLength;
                }
                added += indentLength;

                builder.Append(indent);
                builder.Append(original);
            }
            if (endLine + 1 < lineCount)
            {
                builder.Append(_codeInput.Text.AsSpan(_codeInput.GetCharacterIndexFromLineIndex(endLine + 1)));
            }

            _codeInput.Text = builder.ToString();
            _codeInputScroll.ScrollToVerticalOffset(veritcalScroll);
            _codeInputScroll.ScrollToHorizontalOffset(horizontalScroll);

            int newStart = start + firstAdded;
            int newLength = length + added;
            _codeInput.CaretIndex = newStart;
            _codeInput.SelectionStart = newStart;
            _codeInput.SelectionLength = newLength;
        }

        private void Unindent()
        {
            int start = _codeInput.SelectionStart;
            int startLine = _codeInput.GetLineIndexFromCharacterIndex(start);
            UnindentSingleLine(start, startLine);
            return;

            if (_codeInput.SelectionLength > 0)
            {
                int length = _codeInput.SelectionLength;
                int endLine = _codeInput.GetLineIndexFromCharacterIndex(start + length);
                if (startLine != endLine)
                {
                    UnindentMultiLine(start, length, startLine, endLine);
                    return;
                }
            }
            UnindentSingleLine(start, startLine);
        }

        private void UnindentSingleLine(int start, int startLine)
        {
            int lineLength = _codeInput.GetLineLength(startLine);
            if (lineLength == 0)
            {
                return;
            }

            int lineStart = _codeInput.GetCharacterIndexFromLineIndex(startLine);
            if (start == lineStart)
            {
                return;
            }

            ReadOnlySpan<char> span = _codeInput.Text.AsSpan(lineStart, start - lineStart);
            if (!span.IsWhiteSpace())
            {
                return;
            }

            if (_indentString.Length == 1)
            {
                _codeInput.SelectionStart = start - 1;
                _codeInput.SelectionLength = 1;
                _codeInput.SelectedText = string.Empty;
                _codeInput.SelectionLength = 0;
                _codeInput.CaretIndex = Math.Max(start - 1, start);
            }

            int lineCaretIndex = start - lineStart;
            int targetLength = _indentString.Length - (lineCaretIndex % _indentString.Length);

            _codeInput.SelectionStart = start - targetLength;
            _codeInput.SelectionLength = targetLength;
            _codeInput.SelectedText = string.Empty;
            _codeInput.SelectionLength = 0;
            _codeInput.CaretIndex = Math.Max(start - targetLength, lineStart);
        }

        private void UnindentMultiLine(int start, int length, int startLine, int endLine)
        {
            double veritcalScroll = _codeInputScroll.VerticalOffset;
            double horizontalScroll = _codeInputScroll.HorizontalOffset;
            int lines = endLine - startLine + 1;
            int lineCount = _codeInput.LineCount;
            int firstRemoved = 0;
            int removed = 0;

            StringBuilder builder = new StringBuilder(_codeInput.Text.Length + (_indentString.Length * lines));
            builder.Append(_codeInput.Text.AsSpan(0, _codeInput.GetCharacterIndexFromLineIndex(startLine)));
            for (int i = 0; i < lines; i++)
            {
                if (_codeInput.GetLineLength(startLine + i) == 0)
                {
                    continue;
                }

                string original = _codeInput.GetLineText(startLine + i);
                int index = -1;
                for (int j = 0; j < original.Length; j++)
                {
                    if (j == original.Length || !char.IsWhiteSpace(original[j]))
                    {
                        index = j;
                        break;
                    }
                }

                if (index == 0)
                {
                    builder.Append(original);
                    if (i == 0)
                    {
                        firstRemoved = 0;
                    }
                    continue;
                }

                int indentLength = index % _indentString.Length;
                if (indentLength == 0)
                {
                    indentLength = _indentString.Length;
                }
                if (i == 0)
                {
                    firstRemoved = indentLength;
                }
                removed += indentLength;
                builder.Append(original.AsSpan(indentLength));
            }
            if (endLine + 1 < lineCount)
            {
                builder.Append(_codeInput.Text.AsSpan(_codeInput.GetCharacterIndexFromLineIndex(endLine + 1)));
            }

            _codeInput.Text = builder.ToString();
            _codeInputScroll.ScrollToVerticalOffset(veritcalScroll);
            _codeInputScroll.ScrollToHorizontalOffset(horizontalScroll);

            int newStart = Math.Max(Math.Min(start - firstRemoved, start), 0);
            int newLength = length - removed + firstRemoved;
            _codeInput.CaretIndex = newStart;
            _codeInput.SelectionStart = newStart;
            _codeInput.SelectionLength = newLength;
        }

        private void GoToLineStart(bool select)
        {
            int start = _codeInput.CaretIndex;
            int line = _codeInput.GetLineIndexFromCharacterIndex(start);
            int lineStart = _codeInput.GetCharacterIndexFromLineIndex(line);
            int selectEnd = _codeInput.SelectionStart + _codeInput.SelectionLength;
            int caretStart = start == lineStart ? lineStart + _codeInput.GetLineLength(line) : start;

            ReadOnlySpan<char> span = _codeInput.Text.AsSpan(lineStart, caretStart - lineStart);
            int targetIndex = -1;
            if (!span.IsWhiteSpace())
            {
                for (int i = 0; i < span.Length; i++)
                {
                    if (!char.IsWhiteSpace(span[i]))
                    {
                        targetIndex = lineStart + i;
                        break;
                    }
                }
            }
            else
            {
                targetIndex = lineStart;
            }

            _codeInput.CaretIndex = targetIndex;
            if (select)
            {
                if (start == lineStart)
                {
                    _codeInput.SelectionStart = lineStart;
                    _codeInput.SelectionLength = targetIndex - lineStart;
                }
                else
                {
                    _codeInput.SelectionStart = targetIndex;
                    _codeInput.SelectionLength = selectEnd - targetIndex;
                }
            }
        }

#pragma warning disable IDE1006

        private void _codeInputScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _lineBox.ScrollToVerticalOffset(_codeInputScroll.VerticalOffset / _editorZoom);
        }

        private void _compileButton_Click(object sender, RoutedEventArgs e)
        {
            _analyzer.PreventAnalyzing();
            CompileAndRun();
        }

        private void _toggleOpButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpsVisible)
            {
                HideOps();
            }
            else
            {
                ShowOps();
            }
        }

        private void _codeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int lines = _codeInput.LineCount;
            if (_prevLines != lines)
            {
                _prevLines = lines;
                _lineBox.Clear();
                _lineBuilder.Clear();
                for (int i = 0; i < lines; i++)
                {
                    _lineBuilder.AppendLine((i + 1).ToString());
                }
                _lineBox.Text = _lineBuilder.ToString();
            }

            _analyzer.SetSrc(_codeInput.Text);
        }

        private void _codeInput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int index = _codeInput.CaretIndex + _codeInput.SelectionLength;
            int line = _codeInput.GetLineIndexFromCharacterIndex(index);
            int lineStart = _codeInput.GetCharacterIndexFromLineIndex(line);
            int c = index - lineStart;
            _currLineBox.Text = "Ln: " + (line + 1);
            _currCharBox.Text = "Ch: " + (c + 1);
        }

        private void _toggleAnalyzerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_analyzing)
            {
                _toggleAnalyzerButton.Content = "Disable Analyzer";
                _analyzerThread = new Thread(_analyzer.StartAnalyzing);
                _analyzerThread.IsBackground = true;
                _analyzerThread.Start();
            }
            else
            {
                _toggleAnalyzerButton.Content = "Enable Analyzer";
                _analyzer.StopAnalyzing();
            }
            _analyzing = !_analyzing;
        }

        private void _codeInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    Unindent();
                }
                else
                {
                    Indent();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    GoToLineStart(Keyboard.Modifiers == ModifierKeys.Shift);
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.R)
                {
                    _analyzer.PreventAnalyzing();
                    CompileAndRun();
                }
                else if (e.Key == Key.O)
                {
                    if (OpsVisible)
                    {
                        HideOps();
                    }
                    else
                    {
                        ShowOps();
                    }
                }
            }
        }

        private void _toggleCILButton_Click(object sender, RoutedEventArgs e)
        {
            _toggleCILButton.Content = _cilOutput ? "Enable CIL" : "Disable CIL";
            _cilOutput = !_cilOutput;
            _toggleOpButton.Content = GetOpsButtonString();
        }

        private void _saveOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = "scriptoutput",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("#SCRIPT");
                builder.AppendLine(_prevSrc);
                builder.AppendLine();
                builder.AppendLine("#OUTPUT");
                builder.AppendLine(_outputBox.Text);
                builder.AppendLine(_compiledCil ? "#CIL" : "#BYTECODE");
                builder.AppendLine(_opOutput.Text);

                File.WriteAllText(dialog.FileName, builder.ToString());
            }
        }

#pragma warning restore IDE1006
    }
}