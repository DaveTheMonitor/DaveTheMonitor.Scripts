using System.Text;

namespace DaveTheMonitor.Scripts.Runtime
{
    public static class RuntimeHelper
    {
        public static void TypeError(this IScriptRuntime runtime, string method, string[] expected, ScriptType received)
        {
            TypeError(runtime, method, expected, received.Name);
        }

        public static void TypeError(this IScriptRuntime runtime, string method, string[] expected, ScriptVar received)
        {
            TypeError(runtime, method, expected, received.Type == ScriptVarType.Null ? "null" : received.GetScriptType(runtime.Reference).Name);
        }

        public static void TypeError(this IScriptRuntime runtime, string method, string[] expected, string received)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < expected.Length; i++)
            {
                b.Append(expected[i]);
                if (expected.Length > 0 && i < expected.Length - 1)
                {
                    b.Append(i == expected.Length - 2 ? " or " : ", ");
                }
            }
            runtime.Error(ScriptErrorCode.R_ArgTypeError, "Invalid Type", method != null ? $"{method} expected {b}, received {received}" : $"Expected {b}, received {received}");
        }

        public static void OutOfBoundsError(this IScriptRuntime runtime, ScriptType type, long index)
        {
            runtime.Error(ScriptErrorCode.R_OutOfBounds, "Out Of Bounds Access", $"{type.Name} element {index} is out of bounds.");
        }

        public static void ReadOnlyError(this IScriptRuntime runtime, ScriptType type)
        {
            runtime.Error(ScriptErrorCode.R_ReadonlyCollection, "Readonly Modification", $"{type.Name} cannot be added modified; {type.Name} is readonly.");
        }
    }
}
