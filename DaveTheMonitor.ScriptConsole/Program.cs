using DaveTheMonitor.Scripts;
using DaveTheMonitor.Scripts.Attributes;
using DaveTheMonitor.Scripts.Compiler;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DaveTheMonitor.ScriptConsole
{
    [ScriptType]
    public static class ScriptTest
    {
        [ScriptProperty]
        public static double TestProp => 10.4;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            object v = new object();

            ScriptType.RegisterTypes(new Assembly[] { Assembly.GetExecutingAssembly() });

            var types = ScriptType.GetAllTypes();

            // Add string to test here
            string src = """

""".Replace("\r\n", "\n").Replace('\r', '\n');

            ScriptCompiler compiler = new ScriptCompiler();
            compiler.ErrorHandler += HandleCompilerError;

            compiler.SetSrc(src);
            Script script = compiler.Compile("", ScriptRuntimeType.Mod, CompilerOptimization.Basic, new string[] { "scriptconsole" });

            Console.WriteLine(script.GetBytecodeString(true));

            ScriptRuntime runtime = new ScriptRuntime(1024, 1024, 1024);
            runtime.PrintHandler += HandlePrint;
            runtime.ErrorHandler += HandleRuntimeError;
            Stopwatch timer = new Stopwatch();
            int count = int.Parse(Console.ReadLine());

            bool cil = false;
            if (cil)
            {
                ScriptCILCompiler cilCompiler = new ScriptCILCompiler();
                cilCompiler.OutputCILString = true;
                DynamicMethod method = cilCompiler.Compile(script);
                Console.WriteLine();
                Console.WriteLine(cilCompiler.GetCILOutputString());

                method = cilCompiler.Compile(script);
                Console.WriteLine();
                Console.WriteLine(cilCompiler.GetCILOutputString());
            }

            Console.WriteLine();
            Console.WriteLine("Running...");
            Console.WriteLine();

            for (int i = 0; i < count; i++)
            {
                timer.Restart();
                runtime.RunScript(script);
                timer.Stop();
                Console.WriteLine($"ms: {((timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000):0.0000}");
                Console.WriteLine();
            }
        }

        private static void Equivalent(IScriptRuntime runtime)
        {
            IScriptReference reference = runtime.Reference;
            ScriptVar x = new ScriptVar(0);
            Start:
            ScriptVar count = new ScriptVar(50_000_000);
            ScriptVar result = new ScriptVar(x.CompareTo(count, runtime) >= 0);
            reference.RemoveReference(ref count);
            if (result.GetBoolValue())
            {
                goto End;
            }

            ScriptVar left = x;
            ScriptVar right = new ScriptVar(1);
            x = ScriptVar.Add(left, right, runtime);
            reference.RemoveReference(ref left);
            reference.RemoveReference(ref right);
            goto Start;
            End:
            runtime.Print(x.ToString(runtime.Reference));
            reference.RemoveReference(ref x);
        }

        public static void HandleCompilerError(object sender, ScriptCompilerErrorEventArgs e)
        {
            Debugger.Break();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void HandlePrint(object sender, ScriptPrintEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public static void HandleRuntimeError(object sender, ScriptErrorEventArgs e)
        {
            Debugger.Break();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}