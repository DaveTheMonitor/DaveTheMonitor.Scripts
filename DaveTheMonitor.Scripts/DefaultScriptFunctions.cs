using DaveTheMonitor.Scripts.Attributes;
using DaveTheMonitor.Scripts.Objects;
using System;

namespace DaveTheMonitor.Scripts
{
    [ScriptType]
    public static class DefaultScriptFunctions
    {
        #region Properties

        public static Random Rand { get; set; } = new Random();

        [ScriptConst]
        public const double E = Math.E;

        [ScriptConst]
        public const double Pi = Math.PI;

        [ScriptConst]
        public const double Tau = Math.Tau;

        #endregion

        #region Functions

        // Calls to this method are never actually emitted by the compiler,
        // it converts calls to this to ScriptOp.Print
        [ScriptMethod]
        public static void Print(IScriptRuntime runtime, ScriptVar v)
        {
            runtime.Print(v.ToString(runtime.Reference));
        }

        [ScriptMethod]
        public static double Random(IScriptRuntime runtime, double min, double max)
        {
            if (min > max)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid Random Arguments", "min must be <= max");
                return 0;
            }
            return min + (Rand.NextDouble() * (max - min));
        }

        [ScriptMethod]
        public static long RandomInt(IScriptRuntime runtime, long min, long max)
        {
            if (min >= max)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid RandomInt Arguments", "min must be < max");
                return 0;
            }
            return Rand.NextInt64(min, max);
        }

        #endregion

        #region Constructors

        [ScriptMethod]
        public static ScriptRandom CreateRandom(long seed)
        {
            if (seed == 0)
            {
                seed = Rand.Next(int.MinValue, int.MaxValue);
            }
            return new ScriptRandom((int)seed);
        }

        [ScriptMethod]
        public static ScriptArrayVar CreateArray() => new ScriptArrayVar();

        #endregion

        #region Runtime

        [ScriptMethod]
        public static string GetScriptPermission(IScriptRuntime runtime)
        {
            return runtime.RuntimeType switch
            {
                ScriptRuntimeType.World => "World",
                ScriptRuntimeType.Mod => "Mod",
                _ => "Unknown"
            };
        }

        [ScriptMethod]
        public static long GetReferenceCount(IScriptRuntime runtime, ScriptVar value)
        {
            if (!value.IsRef)
            {
                return 0;
            }
            return runtime.Reference.GetReferenceCount(value.GetObjectId());
        }

        public static void Script(IScriptRuntime runtime, string name)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region String

        [ScriptMethod]
        public static long StringLength(string str)
        {
            return str.Length;
        }

        [ScriptMethod]
        public static ScriptArrayVar StringChars(IScriptRuntime runtime, string str)
        {
            ScriptArrayVar arr = new ScriptArrayVar(str.Length);
            foreach (char c in str)
            {
                arr.Add(runtime, new ScriptVar(c.ToString(), runtime.Reference));
            }
            return arr;
        }

        [ScriptMethod]
        public static string StringCharAt(string str, long index)
        {
            return str[(int)index].ToString();
        }

        [ScriptMethod]
        public static string Substring(IScriptRuntime runtime, string str, long start, long length)
        {
            if (start < 0 || start >= str.Length)
            {
                runtime.Error(ScriptErrorCode.R_OutOfBounds, "Out of Bounds Access", "Index is out of bounds.");
                return null;
            }

            if (length == 0)
            {
                length = str.Length - start;
            }
            else if (start + length > str.Length)
            {
                runtime.Error(ScriptErrorCode.R_OutOfBounds, "Out of Bounds Access", "Index is out of bounds.");
                return null;
            }
            else if (length < 0)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid Length", "Length must be >= 0.");
                return null;
            }
            else if (length == 0)
            {
                return string.Empty;
            }
            return str.Substring((int)start, (int)length);
        }

        [ScriptMethod]
        public static string StringRemove(string str, string value)
        {
            return str.Replace(value, null);
        }

        [ScriptMethod]
        public static string StringRemoveSubstring(IScriptRuntime runtime, string str, long start, long length)
        {
            if (start < 0 || start >= str.Length)
            {
                runtime.Error(ScriptErrorCode.R_OutOfBounds, "Out of Bounds Access", "Index is out of bounds.");
                return null;
            }

            if (length == 0)
            {
                length = str.Length - start;
            }
            else if (start + length > str.Length)
            {
                runtime.Error(ScriptErrorCode.R_OutOfBounds, "Out of Bounds Access", "Index is out of bounds.");
                return null;
            }
            else if (length < 0)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid Length", "Length must be >= 0.");
                return null;
            }
            else if (length == 0)
            {
                return str;
            }

            return str.Remove((int)start, (int)length);
        }

        [ScriptMethod]
        public static string StringTrim(string str)
        {
            return str.Trim();
        }

        [ScriptMethod]
        public static string StringTrimStart(string str)
        {
            return str.TrimStart();
        }

        [ScriptMethod]
        public static string StringTrimEnd(string str)
        {
            return str.TrimEnd();
        }

        [ScriptMethod]
        public static string StringReplace(string str, string oldValue, string newValue)
        {
            return str.Replace(oldValue, newValue);
        }

        [ScriptMethod]
        public static string StringToUpper(string str)
        {
            return str.ToUpperInvariant();
        }

        [ScriptMethod]
        public static string StringToLower(string str)
        {
            return str.ToLowerInvariant();
        }

        [ScriptMethod]
        public static bool StringContains(string str, string value)
        {
            return str.Contains(value);
        }

        [ScriptMethod]
        public static bool StringStartsWith(string str, string value)
        {
            return str.StartsWith(value);
        }

        [ScriptMethod]
        public static bool StringEndsWith(string str, string value)
        {
            return str.EndsWith(value);
        }

        [ScriptMethod]
        public static long StringIndexOf(string str, string value)
        {
            return str.IndexOf(value);
        }

        [ScriptMethod]
        public static long StringLastIndexOf(string str, string value)
        {
            return str.LastIndexOf(value);
        }

        [ScriptMethod]
        public static ScriptArrayVar StringSplit(IScriptRuntime runtime, string str, string separator)
        {
            string[] split = str.Split(separator);
            return new ScriptArrayVar(runtime, split, true);
        }

        [ScriptMethod]
        public static string ToString(IScriptRuntime runtime, ScriptVar v)
        {
            return v.ToString(runtime.Reference);
        }

        #endregion

        #region Type

        [ScriptMethod]
        public static string GetType(IScriptRuntime runtime, ScriptVar value)
        {
            return value.GetScriptType(runtime.Reference).FullName;
        }

        [ScriptMethod]
        public static long GetTypeId(IScriptRuntime runtime, ScriptVar value)
        {
            return value.GetScriptType(runtime.Reference).Id;
        }

        [ScriptMethod]
        public static long GetTypeIdFromName(string fullTypeName)
        {
            int index = fullTypeName.LastIndexOf('.');
            if (index == -1)
            {
                return -1;
            }
            string @namespace = fullTypeName.Substring(0, index).ToLowerInvariant();
            string name = fullTypeName.Substring(index + 1).ToLowerInvariant();
            ScriptType type = ScriptType.GetType(@namespace, name);
            if (type == null)
            {
                return -1;
            }
            return type.Id;
        }

        [ScriptMethod]
        public static bool IsType(IScriptRuntime runtime, ScriptVar value, string fullTypeName)
        {
            int index = fullTypeName.LastIndexOf('.');
            if (index == -1)
            {
                return false;
            }
            string @namespace = fullTypeName.Substring(0, index).ToLowerInvariant();
            string name = fullTypeName.Substring(index + 1).ToLowerInvariant();
            ScriptType type = ScriptType.GetType(@namespace, name);
            if (type == null)
            {
                return false;
            }
            return ScriptType.IsTypeOrSubType(runtime.Reference, value, type);
        }

        [ScriptMethod]
        public static bool IsTypeId(IScriptRuntime runtime, ScriptVar value, long typeId)
        {
            return ScriptType.IsTypeOrSubType(runtime.Reference, value, ScriptType.GetType((int)typeId));
        }

        #endregion

        #region Math

        [ScriptMethod]
        public static double Abs(double x) => Math.Abs(x);

        [ScriptMethod]
        public static double Acos(double x) => Math.Acos(x);

        [ScriptMethod]
        public static double Acosh(double x) => Math.Acosh(x);

        [ScriptMethod]
        public static double Asin(double x) => Math.Asin(x);

        [ScriptMethod]
        public static double Asinh(double x) => Math.Asinh(x);

        [ScriptMethod]
        public static double Atan(double x) => Math.Atan(x);

        [ScriptMethod]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        [ScriptMethod]
        public static double Atanh(double x) => Math.Atanh(x);

        [ScriptMethod]
        public static double Cbrt(double x) => Math.Cbrt(x);

        [ScriptMethod]
        public static double Ceil(double x) => Math.Ceiling(x);

        [ScriptMethod]
        public static double Cos(double x) => Math.Cos(x);

        [ScriptMethod]
        public static double Cosh(double x) => Math.Cosh(x);

        [ScriptMethod]
        public static double Exp(double x) => Math.Exp(x);

        [ScriptMethod]
        public static double Floor(double x) => Math.Floor(x);

        [ScriptMethod]
        public static double Log(double x, double @base) => (double)Math.Log(x, @base);

        [ScriptMethod]
        public static double Log10(double x) => (double)Math.Log10(x);

        [ScriptMethod]
        public static double Log2(double x) => (double)Math.Log2(x);

        [ScriptMethod]
        public static double LogE(double x) => (double)Math.Log(x);

        [ScriptMethod]
        public static double Max(double x, double y) => Math.Max(x, y);

        [ScriptMethod]
        public static double Min(double x, double y) => Math.Min(x, y);

        [ScriptMethod]
        public static double Pow(double x, double y) => Math.Pow(x, y);

        [ScriptMethod]
        public static double Round(double x) => Math.Round(x);

        [ScriptMethod]
        public static long Sign(double x) => Math.Sign(x);

        [ScriptMethod]
        public static double Sin(double x) => Math.Sin(x);

        [ScriptMethod]
        public static double Sinh(double x) => Math.Sinh(x);

        [ScriptMethod]
        public static double Sqrt(double x) => Math.Sqrt(x);

        [ScriptMethod]
        public static double Tan(double x) => Math.Tan(x);

        [ScriptMethod]
        public static double Tanh(double x) => Math.Tanh(x);

        [ScriptMethod]
        public static double Trunc(double x) => Math.Truncate(x);

        [ScriptMethod]
        public static double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(DistanceSquared(x1, y1, z1, x2, y2, z2));
        }

        [ScriptMethod]
        public static double DistanceSquared(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (x1 - x2) * (y1 - y2) * (z1 - z2);
        }

        [ScriptMethod]
        public static double Lerp(double x, double y, double amount)
        {
            return Math.Clamp(x + (y - x), x, y);
        }

        [ScriptMethod]
        public static double ToRadians(double x)
        {
            return x * (Math.PI / 180);
        }

        [ScriptMethod]
        public static double ToDegrees(double x)
        {
            return x * (180 / Math.PI);
        }

        #endregion
    }
}
