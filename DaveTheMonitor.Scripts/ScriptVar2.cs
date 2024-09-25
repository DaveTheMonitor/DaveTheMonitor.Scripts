//using System;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;

//namespace DaveTheMonitor.Scripts
//{
//    public readonly struct ScriptVar
//    {
//        public static ScriptVar Null => _null;
//        public static ScriptVar True => _true;
//        public static ScriptVar False => _false;
//        public static ScriptVar Zero => _zero;
//        public static ScriptVar One => _one;
//        public static string TrueString => "True";
//        public static string FalseString => "False";
//        public static string NullString => "Null";
//        private const long _nanRaw = -2251799813685248L;
//        private const long _trueRaw = -1970324836974591L;
//        private const long _falseRaw = -1970324836974592L;
//        private const long _objectBaseRaw = -1688849860263936L;
//        private const long _stringBaseRaw = -1407374883553280L;
//        private const long _nullRaw = -1125899906842624L;
//        private static ScriptVar _null = new ScriptVar(_nullRaw);
//        private static ScriptVar _true = new ScriptVar(_trueRaw);
//        private static ScriptVar _false = new ScriptVar(_falseRaw);
//        private static ScriptVar _zero = new ScriptVar(0d);
//        private static ScriptVar _one = new ScriptVar(1d);

//        public ScriptVarType Type
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get
//            {
//                if (!double.IsNaN(_value))
//                {
//                    return ScriptVarType.Double;
//                }
//                double d = _value;
//                long v = Unsafe.As<double, long>(ref d);
//                return (ScriptVarType)(((v << 13) >> 61) & 7L);
//            }
//        }
//        public bool IsRef
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get
//            {
//                ScriptVarType type = Type;
//                return type == ScriptVarType.Object || type == ScriptVarType.String;
//            }
//        }
//        public bool IsNull
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => _value == _nullRaw;
//        }
//        private readonly double _value;

//        public static ScriptVar Add(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            if (left.Type == ScriptVarType.Double)
//            {
//                if (right.Type == ScriptVarType.Double)
//                {
//                    return new ScriptVar(AddDouble(left, right));
//                }
//            }
//            else if (left.Type == ScriptVarType.String)
//            {
//                return new ScriptVar(AddString(left, right, runtime), runtime.Reference);
//            }
//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot add {right.Type} to {left.Type}");
//            return Null;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static double AddDouble(ScriptVar left, ScriptVar right)
//        {
//            return left.GetDoubleValue() + right.GetDoubleValue();
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static string AddString(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            return left.GetStringValue(runtime.Reference) + right.ToString(runtime.Reference);
//        }

//        public static double Sub(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
//            {
//                return left.GetDoubleValue() - right.GetDoubleValue();
//            }
//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot subtract {right.Type} from {left.Type}");
//            return 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static double SubDouble(ScriptVar left, ScriptVar right)
//        {
//            return left.GetDoubleValue() - right.GetDoubleValue();
//        }

//        public static double Mul(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
//            {
//                return left.GetDoubleValue() * right.GetDoubleValue();
//            }
//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot multiply {left.Type} with {right.Type}");
//            return 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static double MulDouble(ScriptVar left, ScriptVar right)
//        {
//            return left.GetDoubleValue() * right.GetDoubleValue();
//        }

//        public static double Div(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
//            {
//                return left.GetDoubleValue() / right.GetDoubleValue();
//            }
//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot divide {left.Type} by {right.Type}");
//            return 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static double DivDouble(ScriptVar left, ScriptVar right)
//        {
//            return left.GetDoubleValue() / right.GetDoubleValue();
//        }

//        public static double Mod(ScriptVar left, ScriptVar right, IScriptRuntime runtime)
//        {
//            if (left.Type == ScriptVarType.Double && right.Type == ScriptVarType.Double)
//            {
//                return left.GetDoubleValue() % right.GetDoubleValue();
//            }
//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot modulo {left.Type} and {right.Type}");
//            return 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static double ModDouble(ScriptVar left, ScriptVar right)
//        {
//            return left.GetDoubleValue() % right.GetDoubleValue();
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private long GetRawValue()
//        {
//            double v = _value;
//            return Unsafe.As<double, long>(ref v);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public int GetIntValue()
//        {
//            return (int)GetDoubleValue();
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public double GetDoubleValue()
//        {
//            return _value;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public bool GetBoolValue()
//        {
//            double d = _value;
//            long v = Unsafe.As<double, long>(ref d);
//            return (v << 32) == 4294967296L;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public IScriptObject GetObjectValue(IScriptReference reference)
//        {
//            return IsNull ? null : (IScriptObject)reference.GetObject(GetObjectId());
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public string GetStringValue(IScriptReference reference)
//        {
//            return IsNull ? null : (string)reference.GetObject(GetObjectId());
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public int GetObjectId()
//        {
//            double d = _value;
//            long v = Unsafe.As<double, long>(ref d);
//            return (int)((v << 32) >> 32);
//        }

//        public ScriptType GetScriptType(IScriptReference reference)
//        {
//            return Type switch
//            {
//                ScriptVarType.Null => ScriptType.GetType(0),
//                ScriptVarType.Double => ScriptType.GetType(GetDoubleValue() == (int)GetDoubleValue() ? 3 : 2),
//                ScriptVarType.Bool => ScriptType.GetType(4),
//                ScriptVarType.String => ScriptType.GetType(5),
//                _ => reference.GetObjectType(GetObjectId())
//            };
//        }

//        public string ToString(IScriptReference reference)
//        {
//            return Type switch
//            {
//                ScriptVarType.Double => GetDoubleValue().ToString(),
//                ScriptVarType.Bool => GetBoolValue() ? TrueString : FalseString,
//                ScriptVarType.String => (string)reference.GetObject(GetObjectId()),
//                ScriptVarType.Object => ((IScriptObject)reference.GetObject(GetObjectId())).ScriptToString(),
//                _ => NullString
//            };
//        }

//        public override string ToString()
//        {
//            return Type switch
//            {
//                ScriptVarType.Double => GetDoubleValue().ToString(),
//                ScriptVarType.Bool => GetBoolValue() ? TrueString : FalseString,
//                ScriptVarType.String => "{String}",
//                ScriptVarType.Object => "{Object}",
//                _ => NullString
//            };
//        }

//        public bool Equals(ScriptVar other, IScriptReference reference)
//        {
//            if (Type != other.Type)
//            {
//                return false;
//            }
//            if (IsRef && other.IsRef)
//            {
//                return reference.GetReference(_value) == reference.GetReference(other._value);
//            }
//            return _value == other._value;
//        }

//        public int CompareTo(ScriptVar other, IScriptRuntime runtime)
//        {
//            if (Type == ScriptVarType.Double && other.Type == ScriptVarType.Double)
//            {
//                double left = GetDoubleValue();
//                double right = other.GetDoubleValue();
//                if (left < right)
//                {
//                    return -1;
//                }
//                else if (left > right)
//                {
//                    return 1;
//                }
//                else
//                {
//                    return 0;
//                }
//            }

//            runtime.Error(ScriptErrorCode.R_InvalidOperand, "Invalid Types", $"Cannot compare {Type} with {other.Type}");
//            return 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static ScriptVar CreateString(int id)
//        {
//            return new ScriptVar(_stringBaseRaw + id);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static ScriptVar CreateObject(int id)
//        {
//            return new ScriptVar(_objectBaseRaw + id);
//        }

//        public ScriptVar(double value)
//        {
//            _value = value;
//        }

//        public ScriptVar(bool value)
//        {
//            long v = value ? _trueRaw : _falseRaw;
//            _value = Unsafe.As<long, double>(ref v);
//        }

//        public ScriptVar(IScriptObject value, IScriptReference reference)
//        {
//            if (value == null)
//            {
//                _value = _nullRaw;
//                return;
//            }
//            int id = reference.AddReference(value);
//            long v = _objectBaseRaw + id;
//            _value = Unsafe.As<long, double>(ref v);
//        }

//        public ScriptVar(string value, IScriptReference reference)
//        {
//            if (value == null)
//            {
//                _value = _nullRaw;
//                return;
//            }
//            int id = reference.AddReference(value);
//            long v = _stringBaseRaw + id;
//            _value = Unsafe.As<long, double>(ref v);
//        }

//        private ScriptVar(long rawValue)
//        {
//            _value = Unsafe.As<long, double>(ref rawValue);
//        }
//    }
//}
