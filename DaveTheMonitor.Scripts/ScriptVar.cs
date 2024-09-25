using System.Runtime.CompilerServices;

namespace DaveTheMonitor.Scripts
{
    public readonly struct ScriptVar
    {
        public static ScriptVar Null => new ScriptVar();
        public static ScriptVar True => new ScriptVar(1, ScriptVarType.Bool);
        public static ScriptVar False => new ScriptVar(0, ScriptVarType.Bool);
        public static ScriptVar Zero => new ScriptVar(0);
        public static ScriptVar One => new ScriptVar(1);
        public static string TrueString => "True";
        public static string FalseString => "False";
        public static string NullString => "Null";

        public ScriptVarType Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type;
        }
        public bool IsRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type == ScriptVarType.Object || _type == ScriptVarType.String;
        }
        private readonly ScriptVarType _type;
        private readonly long _value;

        public static ScriptVar Add(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
            {
                return new ScriptVar(AddDouble(left, right));
            }
            else if (left.Type == ScriptVarType.String || right.Type == ScriptVarType.String)
            {
                return new ScriptVar(AddString(left, right, runtime), runtime.Reference);
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot add {right.Type} to {left.Type}");
            return Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AddDouble(ScriptVar left, ScriptVar right)
        {
            return left.GetDoubleValue() + right.GetDoubleValue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AddString(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            return left.ToString(runtime.Reference) + right.ToString(runtime.Reference);
        }

        public static double Sub(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
            {
                return left.GetDoubleValue() - right.GetDoubleValue();
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot subtract {right.Type} from {left.Type}");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SubDouble(ScriptVar left, ScriptVar right)
        {
            return left.GetDoubleValue() - right.GetDoubleValue();
        }

        public static double Mul(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
            {
                return left.GetDoubleValue() * right.GetDoubleValue();
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot multiply {left.Type} with {right.Type}");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MulDouble(ScriptVar left, ScriptVar right)
        {
            return left.GetDoubleValue() * right.GetDoubleValue();
        }

        public static double Div(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
            {
                return left.GetDoubleValue() / right.GetDoubleValue();
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot divide {left.Type} by {right.Type}");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DivDouble(ScriptVar left, ScriptVar right)
        {
            return left.GetDoubleValue() / right.GetDoubleValue();
        }

        public static double Mod(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
        {
            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
            {
                return left.GetDoubleValue() % right.GetDoubleValue();
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot modulo {left.Type} and {right.Type}");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ModDouble(ScriptVar left, ScriptVar right)
        {
            return left.GetDoubleValue() % right.GetDoubleValue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetRawValue()
        {
            return _value;
        }

        public int GetObjectId() => (int)GetRawValue();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetLongValue()
        {
            return (long)GetDoubleValue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDoubleValue()
        {
            long v = _value;
            return Unsafe.As<long, double>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBoolValue()
        {
            return _value > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IScriptObject GetObjectValue(IScriptReference reference)
        {
            return Type == ScriptVarType.Null ? null : (IScriptObject)reference.GetObject((int)_value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetStringValue(IScriptReference reference)
        {
            return Type == ScriptVarType.Null ? null : (string)reference.GetObject((int)_value);
        }

        public ScriptType GetScriptType(IScriptReference reference)
        {
            return Type switch
            {
                ScriptVarType.Null => ScriptType.GetType(0),
                ScriptVarType.Double => ScriptType.GetType(GetDoubleValue() == (int)GetDoubleValue() ? 3 : 2),
                ScriptVarType.Bool => ScriptType.GetType(4),
                ScriptVarType.String => ScriptType.GetType(5),
                _ => reference.GetObjectType((int)_value)
            };
        }

        public string ToString(IScriptReference reference)
        {
            return Type switch
            {
                ScriptVarType.Double => GetDoubleValue().ToString(),
                ScriptVarType.Bool => _value > 0 ? TrueString : FalseString,
                ScriptVarType.String => (string)reference.GetObject((int)_value),
                ScriptVarType.Object => ((IScriptObject)reference.GetObject((int)_value)).ScriptToString(),
                _ => NullString
            };
        }

        public override string ToString()
        {
            return Type switch
            {
                ScriptVarType.Double => GetDoubleValue().ToString(),
                ScriptVarType.Bool => _value > 0 ? TrueString : FalseString,
                ScriptVarType.String => "{String}",
                ScriptVarType.Object => "{Object}",
                _ => NullString
            };
        }

        public bool Equals(ScriptVar other, IScriptReference reference)
        {
            if (Type != other.Type)
            {
                return false;
            }
            if (IsRef && other.IsRef)
            {
                return reference.GetReference(_value) == reference.GetReference(other._value);
            }
            return _value == other._value;
        }

        public int CompareTo(ScriptVar other, IScriptRuntime runtime)
        {
            if (Type == ScriptVarType.Double && other.Type == ScriptVarType.Double)
            {
                double left = GetDoubleValue();
                double right = other.GetDoubleValue();
                if (left < right)
                {
                    return -1;
                }
                else if (left > right)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot compare {Type} with {other.Type}");
            return 0;
        }

        public static ScriptVar CreateString(int id)
        {
            return new ScriptVar(id, ScriptVarType.String);
        }

        public static ScriptVar CreateObject(int id)
        {
            return new ScriptVar(id, ScriptVarType.Object);
        }

        public ScriptVar(double value)
        {
            _value = Unsafe.As<double, long>(ref value);
            _type = ScriptVarType.Double;
        }

        public ScriptVar(bool value)
        {
            _value = Unsafe.As<bool, byte>(ref value);
            _type = ScriptVarType.Bool;
        }

        public ScriptVar(IScriptObject value, IScriptReference reference)
        {
            if (value == null)
            {
                _type = ScriptVarType.Null;
                return;
            }
            _value = reference.AddReference(value);
            _type = ScriptVarType.Object;
        }

        public ScriptVar(string value, IScriptReference reference)
        {
            if (value == null)
            {
                _type = ScriptVarType.Null;
                return;
            }
            _value = reference.AddReference(value);
            _type = ScriptVarType.String;
        }

        public ScriptVar(int rawValue, ScriptVarType type)
        {
            _value = rawValue;
            _type = type;
        }
    }
}
