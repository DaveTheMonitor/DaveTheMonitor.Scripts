using DaveTheMonitor.Scripts.Compiler;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DaveTheMonitor.ScriptTests
{
    [TestClass]
    public class CompilerTests
    {
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            ScriptType.RegisterTypes(new Assembly[] { Assembly.GetExecutingAssembly() });
        }

        public static ScriptRuntime CreateRuntime()
        {
            return new ScriptRuntime(128, 128, 128);
        }

        public static ScriptCompiler CreateCompiler()
        {
            return new ScriptCompiler();
        }

        private static void CompileAndRun(string src, ScriptRuntimeType type, ScriptCompiler compiler, IScriptRuntime runtime, bool cil, params ScriptInVar[] inVars)
        {
            compiler.SetSrc(src);
            Script script = compiler.Compile("", type, CompilerOptimization.Basic);

            if (script == null)
            {
                return;
            }

            if (cil)
            {
                ScriptCILCompiler cilCompiler = new ScriptCILCompiler();
                cilCompiler.Compile(script);
            }

            if (inVars != null)
            {
                runtime.RunScript(script, inVars);
            }
            else
            {
                runtime.RunScript(script);
            }
        }

        private static void RunAndAssertResults(string src, ScriptRuntimeType type, ScriptInVar[] inVars, ScriptErrorEventHandler errorHandler, ScriptCompilerErrorEventHandler compilerErrorHandler, params string[] expected)
        {
            ScriptRuntime runtime = CreateRuntime();
            ScriptCompiler compiler = CreateCompiler();
            List<string> results = new List<string>();
            runtime.PrintHandler += (object sender, ScriptPrintEventArgs e) => results.Add(e.Message);
            runtime.ErrorHandler += errorHandler;
            compiler.ErrorHandler += compilerErrorHandler;

            CompileAndRun(src, type, compiler, runtime, false, inVars);

            if (expected != null)
            {
                CollectionAssert.AreEqual(new List<string>(expected), results);
            }
        }

        private static void FailOnError(object sender, ScriptErrorEventArgs e)
        {
            Assert.Fail($"{e.Header} : {e.Header} : {e.Message}");
        }

        private static void FailOnCompilerError(object sender, ScriptCompilerErrorEventArgs e)
        {
            Assert.Fail($"{e.Severity} : {e.Header} : {e.Message}");
        }

        [TestMethod]
        public void BasicMath()
        {

            string src = """
Var [x] = [10]
Var [y] = [20]
Print [x] // 10
Print [y] // 20

Var [z] = [x] + [y]
Print [z] // 30

Var [z] = [x] - [y]
Print [z] // -10

Var [z] = [x] * [y]
Print [z] // 200

Var [z] = [x] / [y]
Print [z] // 0.5

Var [z] = [x] % [y]
Print [z] // 10

Var [z] = [Min [x] [y]]
Print [z] // 10

Var [z] += [15]
Print [z] // 25

Var [z] -= [10]
Print [z] // 15
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "10",
                "20",
                "30",
                "-10",
                "200",
                "0.5",
                "10",
                "10",
                "25",
                "15");
        }

        [TestMethod]
        public void Methods()
        {

            string src = """
Var [x] = [Abs [-2]]
Print [x] // 2
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "2");
        }

        [TestMethod]
        public void Properties()
        {

            string src = """
Var [arr] = [new Array]
Print [arr:Count] // 0
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "0");
        }

        [TestMethod]
        public void Constants()
        {

            string src = """
Var [x] = [Pi:]
Print [x] // ~3.14
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                Math.PI.ToString());
        }

        [TestMethod]
        public void Arrays()
        {

            string src = """
Var [arr] = [new Array]
arr:Add [10]
arr:Add [true]
arr:Add ["MyString"]

Var [i] = [arr:IndexOf ["MyString"]]
Print [i] // 2

Var [i] = [arr:IndexOf [10]]
Print [i] // 0

Var [i] = [arr:IndexOf [true]]
Print [i] // 1

Print [arr:Count] // 3

Var [i] = [arr:Contains [15]]
Print [i] // False

Var [i] = [arr:Contains ["MyString"]]
Print [i] // True

arr:Clear
Print [arr:Count] // 0
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "2",
                "0",
                "1",
                "3",
                ScriptVar.FalseString,
                ScriptVar.TrueString,
                "0");
        }

        [TestMethod]
        public void Constructors()
        {

            string src = """
Var [arr] = [new Array]

Print [arr] // array[0]

Var [rand] = [new Random [70]]

Print [rand] // random[70]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "array[0]",
                "random[70]");
        }

        [TestMethod]
        public void Using()
        {

            string src = """
Using [ScriptTests]
Var [x] = [TestProp:]
Print [x] // 15
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "15");
        }

        [TestMethod]
        public void InvalidMember()
        {

            string src = """
// Compiler Error
Var [x] = [TestProp:]
Print [x]
""";

            bool errored = false;
            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, (object sender, ScriptCompilerErrorEventArgs e) =>
            {
                if (e.Header != "Invalid Member")
                {
                    Assert.Fail($"{e.Severity} : {e.Header} : {e.Message}");
                }

                errored = true;
            }, null);

            if (!errored)
            {
                Assert.Fail($"Compiler did not error");
            }
        }

        [TestMethod]
        public void If()
        {

            string src = """
Var [x] = [10]
If
    [x] == [10]
Then
    Print ["x == 10"]
Else
    Print ["x != 10"]
End

Var [x] = [20]
If
    [x] == [15]
Then
    Print ["x == 15"]
Else
    Print ["x != 15"]
End
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "x == 10",
                "x != 15");
        }

        [TestMethod]
        public void Expressions()
        {

            string src = """
Var [x] = [10]
Var [y] = [15]
Var [z] = [[x] == [y]]
Print [z]

Var [x] = [20]
Var [y] = [76]
Var [z] = [[x] != [y]]
Print [z]

Var [x] = [5.6]
Var [y] = [8.6]
Var [z] = [[x] < [y]]
Print [z]

Var [x] = [2.9]
Var [y] = [4.2]
Var [z] = [[x] > [y]]
Print [z]

Var [x] = [5]
Var [y] = [5]
Var [z] = [[x] >= [y]]
Print [z]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                ScriptVar.FalseString,
                ScriptVar.TrueString,
                ScriptVar.TrueString,
                ScriptVar.FalseString,
                ScriptVar.TrueString);
        }

        [TestMethod]
        public void Comments()
        {

            string src = """
# New line comment
/* multi-line
comment */
Var [x] = [10] // Same line comment
Var [y] = /* comment */ [20]
Print [[x] + [y]]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "30");
        }

        [TestMethod]
        public void WhileLoop()
        {

            string src = """
Var [x] = [0]
Var [y] = [0]
Var [i] = [0]
While
    [x] < [3]
Do
    While
        [y] < [3]
    Do
        Print [[ToString [x]] + [", "] + [y]]
        Var [y]++
        Var [i]++
    End
    Var [x]++
    Var [y] = [0]
End
Print [i]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "0, 0",
                "0, 1",
                "0, 2",
                "1, 0",
                "1, 1",
                "1, 2",
                "2, 0",
                "2, 1",
                "2, 2",
                "9");
        }

        [TestMethod]
        public void ForLoop()
        {

            string src = """
For
    Var [x] = [0]
    [x] < [5]
    Var [x]++
Do
    Print [x]
End
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "0",
                "1",
                "2",
                "3",
                "4");
        }

        [TestMethod]
        public void ForeachLoop()
        {

            string src = """
Var [arr] = [CreateArray:]
arr:Add [20]
arr:Add ["MyString"]
arr:Add [null]
arr:Add [false]

Foreach
    Var [item] in [arr]
Do
    Print [item]
End
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "20",
                "MyString",
                ScriptVar.NullString,
                ScriptVar.FalseString);
        }

        [TestMethod]
        public void WhileBreak()
        {

            string src = """
Var [x] = [0]
While
    [x] < [10]
Do
    If
        [x] == [3]
    Then
        Break
    End
    Print [x]
    Var [x]++
End
Print ["End"]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "0",
                "1",
                "2",
                "End");
        }

        [TestMethod]
        public void ForContinue()
        {

            string src = """
For
    Var [x] = [0]
    [x] < [10]
    Var [x]++
Do
    If
        [[x] % [2]] == [0]
    Then
        Continue
    End
    Print [x]
End
Print ["End"]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "1",
                "3",
                "5",
                "7",
                "9",
                "End");
        }

        [TestMethod]
        public void Loop()
        {

            string src = """
Loop [3]
    Print ["Looping"]
End
Print ["End"]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "Looping",
                "Looping",
                "Looping",
                "End");
        }

        [TestMethod]
        public void DynamicVars()
        {

            string src = """
Var [x] = [10]
Print [x]
If
    [true]
Then
    Var [x] = ["Test"]
    Loop [1]
        Var [x] = [10]
    End
    Print [x]
End
Print [x]

Var [x] = [10]
Print [x]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError,
                "10",
                "10",
                "10",
                "10");
        }

        [TestMethod]
        public void DynamicVarMethodCalls()
        {

            string src = """
Var [x] = [10]
Print [x]
If
    [false]
Then
    Var [x] = ["Test"]
End

CreateRandom [x]
StringLength [x]
""";

            bool errored = false;
            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, (object sender, ScriptErrorEventArgs e) =>
            {
                // These two errors are expected but not guaranteed depending on runtime
                // implementation
                if (e.Header == "Stack Not Empty" || e.Header == "Reference Not Removed")
                {
                    return;
                }

                if (e.Header != "Invalid Type")
                {
                    Assert.Fail($"{e.Severity} : {e.Header} : {e.Message}");
                }

                errored = true;
            }, FailOnCompilerError, null);

            if (!errored)
            {
                Assert.Fail($"Runtime did not error");
            }

            src = """
// Compiler error
Var [x] = [10]
Print [x]
If
    [false]
Then
    Var [x] = ["Test"]
    CreateRandom [x]
End

StringLength [x]
""";

            errored = false;
            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, (object sender, ScriptCompilerErrorEventArgs e) =>
            {
                if (e.Header != "Argument Error")
                {
                    Assert.Fail($"{e.Severity} : {e.Header} : {e.Message}");
                }

                errored = true;
            }, null);

            if (!errored)
            {
                Assert.Fail($"Compiler did not error");
            }
        }

        [TestMethod]
        public void InVarsSet()
        {

            string src = """
In [testinvar]

Print [testinvar]
""";

            ScriptInVar[] vars = new ScriptInVar[]
            {
                new ScriptInVar("testinvar", 10)
            };
            RunAndAssertResults(src, ScriptRuntimeType.Mod, vars, FailOnError, FailOnCompilerError, "10");
        }

        [TestMethod]
        public void InVarsNull()
        {

            string src = """
In [testinvar]

Print [testinvar]
""";

            RunAndAssertResults(src, ScriptRuntimeType.Mod, null, FailOnError, FailOnCompilerError, ScriptVar.NullString);
        }
    }
}