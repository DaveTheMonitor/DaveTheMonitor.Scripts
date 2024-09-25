using DaveTheMonitor.Scripts.Runtime;
using DaveTheMonitor.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DaveTheMonitor.Scripts.Compiler
{
    public sealed class ScriptCILCompiler : IDisposable
    {
        private enum LocalType
        {
            Local,
            InVar
        }

        [DebuggerDisplay("{Type} : {Index}")]
        private struct Local
        {
            public byte Index;
            public LocalType Type;

            public Local(byte index, LocalType type)
            {
                Index = index;
                Type = type;
            }
        }

        [DebuggerDisplay("{Label} : {Position}")]
        private struct ScriptLabel
        {
            public Label Label;
            public int Position;

            public ScriptLabel(Label label, int position)
            {
                Label = label;
                Position = position;
            }
        }

        private static Type _scriptVar = typeof(ScriptVar);
        private static Type _scriptRuntime = typeof(IScriptRuntime);
        private static Type _scriptReference = typeof(IScriptReference);
        private static ConstructorInfo _scriptVarCtorDouble = _scriptVar.GetConstructor(new Type[] { typeof(double) });
        private static MethodInfo _getScriptReference = _scriptRuntime.GetProperty(nameof(IScriptRuntime.Reference), _publicInstance).GetMethod;
        private static MethodInfo _error = _scriptRuntime.GetMethod(nameof(IScriptRuntime.Error), _publicInstance);
        private static MethodInfo _removeReference = _scriptReference.GetMethod(nameof(IScriptReference.RemoveReference), _publicInstance, new Type[] { typeof(ScriptVar).MakeByRefType() });
        private static MethodInfo _addReference = _scriptReference.GetMethod(nameof(IScriptReference.AddReference), _publicInstance, new Type[] { typeof(ScriptVar).MakeByRefType() });
        private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags _publicInstance = BindingFlags.Public | BindingFlags.Instance;

        public bool OutputCILString { get; set; }

        private Action[] _commands;
        private UnsafeByteReader _byteReader;
        private ILGenerator _il;
        private short[] _inVarLookup;
        private int _inVars;
        private List<ScriptLabel> _labels;
        private Dictionary<Type, Stack<int>> _tempLocals;
        private StringBuilder _stringBuilder;
        private Script _script;
        private List<LocalBuilder> _locals;
        private int _referenceLocal;
        private int _stack;
        private bool _disposedValue;
        private List<(int, Label)> _labelsToWrite;
        private Dictionary<Label, int> _labelPositions;

        public DynamicMethod Compile(Script script)
        {
            _byteReader.SetBytes(script.Bytecode);
            _inVars = script.InVars;
            _labels.Clear();
            _tempLocals.Clear();
            _locals.Clear();
            _referenceLocal = -1;
            _script = script;
            _stack = 0;
            if (_inVarLookup.Length < _inVars)
            {
                _inVarLookup = new short[_inVars];
            }
            Array.Fill(_inVarLookup, (short)-1);

            ScriptInVarDefinition[] inVars = script.GetAllInVars();
            for (int i = 0; i < inVars.Length; i++)
            {
                _inVarLookup[inVars[i].LocalIndex] = (short)i;
            }

            Type[] paramTypes;
            if (inVars.Length == 0)
            {
                paramTypes = new Type[] { typeof(IScriptRuntime) };
            }
            else if (inVars.Length <= 4)
            {
                List<Type> types = new List<Type>(inVars.Length + 1) { typeof(IScriptRuntime) };
                for (int i = 0; i < inVars.Length; i++)
                {
                    types.Add(_scriptVar);
                }
                paramTypes = types.ToArray();
            }
            else
            {
                paramTypes = new Type[] { typeof(IScriptRuntime), typeof(ScriptVar[]) };
            }

            _labelPositions ??= new Dictionary<Label, int>();
            int builderIndex;
            if (OutputCILString)
            {
                _labelsToWrite ??= new List<(int, Label)>();
                _stringBuilder ??= new StringBuilder();
                _labelsToWrite.Clear();
                _labelPositions.Clear();
                _stringBuilder.Clear();
                builderIndex = _stringBuilder.Length;
            }
            else
            {
                builderIndex = 0;
            }

            DynamicMethod method = new DynamicMethod($"Script_{script.Name}", typeof(void), paramTypes);
            _il = method.GetILGenerator();
            for (int i = 0; i < script.Locals; i++)
            {
                DeclareLocal(typeof(ScriptVar));
            }

            FindLabels();

            while (_byteReader.Position < _byteReader.Length)
            {
                CompileOp((ScriptOp)_byteReader.ReadByte());
            }

            if (OutputCILString)
            {
                int totalLabelLength = 0;
                foreach ((int, Label) pair in _labelsToWrite)
                {
                    _stringBuilder.Insert(pair.Item1 + totalLabelLength, GetILOffsetString(_labelPositions[pair.Item2]));
                    totalLabelLength += 7;
                }

                if (_locals.Count > 0)
                {
                    _stringBuilder.Insert(builderIndex, $".locals init ({Environment.NewLine}    {string.Join($",{Environment.NewLine}    ", _locals.Select(l => $"[{l.LocalIndex}] {GetCILTypeNamePrefix(l.LocalType)}{GetCILTypeName(l.LocalType)}"))}{Environment.NewLine}){Environment.NewLine}{Environment.NewLine}");
                    _stringBuilder.AppendLine();
                }
            }

            switch (inVars.Length)
            {
                case 0: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime>>(); break;
                case 1: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime, ScriptVar>>(); break;
                case 2: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime, ScriptVar, ScriptVar>>(); break;
                case 3: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime, ScriptVar, ScriptVar, ScriptVar>>(); break;
                case 4: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime, ScriptVar, ScriptVar, ScriptVar, ScriptVar>>(); break;
                default: script.CILInvoker = method.CreateDelegate<Action<IScriptRuntime, ScriptVar[]>>(); break;
            }

            _il = null;
            _labels.Clear();
            _tempLocals.Clear();
            _locals.Clear();
            return method;
        }

        private void FindLabels()
        {
            while (_byteReader.Position < _byteReader.Length)
            {
                ScriptOp op = (ScriptOp)_byteReader.ReadByte();
                if (!ScriptOpHelper.IsJump(op))
                {
                    int bytes = ScriptOpHelper.GetBytes(op);
                    if (bytes > 0)
                    {
                        _byteReader.SkipBytes(bytes);
                    }

                    continue;
                }

                _labels.Add(new ScriptLabel(_il.DefineLabel(), _byteReader.ReadInt32()));
            }

            _byteReader.MoveToStart();
        }

        private string GetOpCodeString(OpCode opCode)
        {
            return $"{GetILOffsetString(_il.ILOffset)}: {opCode}";
        }

        private static string GetCILTypeName(Type type)
        {
            if (type == typeof(int))
            {
                return "int32";
            }
            else if (type == typeof(long))
            {
                return "int64";
            }
            else if (type == typeof(double))
            {
                return "float64";
            }
            else if (type == typeof(void))
            {
                return "void";
            }
            else if (type == typeof(bool))
            {
                return "bool";
            }
            else
            {
                return type.Name;
            }
        }

        private void CompileOp(ScriptOp op)
        {
            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                if (_byteReader.Position - 1 != _labels[i].Position)
                {
                    continue;
                }

                MarkLabel(_labels[i].Label);
            }
            _commands[(int)op]();
        }

        private void Emit(OpCode opCode)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)}");
            }
            _il.Emit(opCode);
        }

        private void Emit(OpCode opCode, int arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, byte arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, sbyte arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, short arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, ushort arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, double arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, string arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} \"{arg}\"");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, Label arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.Append($"{GetOpCodeString(opCode)} ");
                _labelsToWrite.Add((_stringBuilder.Length, arg));
                _stringBuilder.AppendLine();
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, ConstructorInfo arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {GetCILMethodString(arg)}");
            }
            _il.Emit(opCode, arg);
        }

        private void Emit(OpCode opCode, MethodInfo method)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {GetCILMethodString(method)}");
            }
            _il.Emit(opCode, method);
        }

        private void Emit(OpCode opCode, Type arg)
        {
            if (OutputCILString)
            {
                _stringBuilder.AppendLine($"{GetOpCodeString(opCode)} {arg.Name}");
            }
            _il.Emit(opCode, arg);
        }

        private void MarkLabel(Label label)
        {
            _il.MarkLabel(label);
            _labelPositions.Add(label, _il.ILOffset);
        }

        private void EmitLdc_I4(int value)
        {
            switch (value)
            {
                case -1: Emit(OpCodes.Ldc_I4_M1); break;
                case 0: Emit(OpCodes.Ldc_I4_0); break;
                case 1: Emit(OpCodes.Ldc_I4_1); break;
                case 2: Emit(OpCodes.Ldc_I4_2); break;
                case 3: Emit(OpCodes.Ldc_I4_3); break;
                case 4: Emit(OpCodes.Ldc_I4_4); break;
                case 5: Emit(OpCodes.Ldc_I4_5); break;
                case 6: Emit(OpCodes.Ldc_I4_6); break;
                case 7: Emit(OpCodes.Ldc_I4_7); break;
                case 8: Emit(OpCodes.Ldc_I4_8); break;
                case > -128 and < 128: Emit(OpCodes.Ldc_I4_S, (sbyte)value); break;
                default: Emit(OpCodes.Ldc_I4, value); break;
            }
        }

        private void EmitLdloc(int local)
        {
            switch (local)
            {
                case 0: Emit(OpCodes.Ldloc_0); break;
                case 1: Emit(OpCodes.Ldloc_1); break;
                case 2: Emit(OpCodes.Ldloc_2); break;
                case 3: Emit(OpCodes.Ldloc_3); break;
                case < 256: Emit(OpCodes.Ldloc_S, (byte)local); break;
                default: Emit(OpCodes.Ldloc, local); break;
            }
        }

        private void EmitLdloca(int local)
        {
            if (local < 256)
            {
                Emit(OpCodes.Ldloca_S, (byte)local);
            }
            else
            {
                Emit(OpCodes.Ldloca, local);
            }
        }

        private void EmitStloc(int local)
        {
            switch (local)
            {
                case 0: Emit(OpCodes.Stloc_0); break;
                case 1: Emit(OpCodes.Stloc_1); break;
                case 2: Emit(OpCodes.Stloc_2); break;
                case 3: Emit(OpCodes.Stloc_3); break;
                case < 256: Emit(OpCodes.Stloc_S, (byte)local); break;
                default: Emit(OpCodes.Stloc, local); break;
            }
        }

        private void EmitGetScriptReference()
        {
            if (_referenceLocal != -1)
            {
                EmitLdloc(_referenceLocal);
                return;
            }

            _referenceLocal = DeclareLocal(typeof(IScriptReference)).LocalIndex;
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Callvirt, _getScriptReference);
            Emit(OpCodes.Dup);
            EmitStloc(_referenceLocal);
        }

        private static string GetCILMethodString(MethodInfo method)
        {
            return $"{(!method.IsStatic ? "instance " : string.Empty)}{GetCILTypeNamePrefix(method.ReturnType)}{GetCILTypeName(method.ReturnType)} {GetCILTypeName(method.DeclaringType)}::{method.Name}({string.Join(", ", method.GetParameters().Select(p => GetCILTypeName(p.ParameterType)))})";
        }

        private string GetCILMethodString(ConstructorInfo method)
        {
            return $"instance void {GetCILTypeName(method.DeclaringType)}::.ctor({string.Join(", ", method.GetParameters().Select(p => GetCILTypeName(p.ParameterType)))})";
        }

        private static string GetCILTypeNamePrefix(Type type)
        {
            if (type == typeof(void) || type.IsPrimitive)
            {
                return string.Empty;
            }

            if (type.IsValueType)
            {
                return "valuetype ";
            }
            else
            {
                return "class ";
            }
        }

        private static string GetILOffsetString(int offset)
        {
            return $"IL_{Convert.ToString(offset, 16).PadLeft(4, '0')}";
        }

        private void Swap<TTop, TBottom>()
        {
            int topLocal = GetTempLocal<TTop>();
            int bottomLocal = GetTempLocal<TBottom>();
            EmitStloc(topLocal);
            EmitStloc(bottomLocal);
            EmitLdloc(topLocal);
            EmitLdloc(bottomLocal);
            ReleaseTempLocal<TTop>(topLocal);
            ReleaseTempLocal<TBottom>(bottomLocal);
        }

        private void SwapTopByRef<TTop, TBottom>()
        {
            int topLocal = GetTempLocal<TTop>();
            int bottomLocal = GetTempLocal<TBottom>();
            EmitStloc(topLocal);
            EmitStloc(bottomLocal);
            EmitLdloc(topLocal);
            EmitLdloca(bottomLocal);
            ReleaseTempLocal<TTop>(topLocal);
            ReleaseTempLocal<TBottom>(bottomLocal);
        }

        private int GetTempLocal<T>()
        {
            if (_tempLocals.TryGetValue(typeof(T), out Stack<int> stack))
            {
                if (stack.TryPop(out int index))
                {
                    return index;
                }

                return DeclareLocal(typeof(T)).LocalIndex;
            }

            _tempLocals.Add(typeof(T), new Stack<int>());
            return DeclareLocal(typeof(T)).LocalIndex;
        }

        private LocalBuilder DeclareLocal(Type type)
        {
            LocalBuilder local = _il.DeclareLocal(type);
            _locals.Add(local);
            return local;
        }

        private void ReleaseTempLocal<T>(int local)
        {
            _tempLocals[typeof(T)].Push(local);
        }

        private void CompileNop()
        {
            
        }

        private void CompileNullLiteral()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Null), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileDoubleLiteral()
        {
            Emit(OpCodes.Ldc_R8, _byteReader.ReadDouble());
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack++;
        }

        private void CompileDoubleLiteral_0()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Zero), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileDoubleLiteral_1()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.One), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileLongLiteral()
        {
            Emit(OpCodes.Ldc_R8, (double)_byteReader.ReadInt64());
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack++;
        }

        private void CompileLongLiteral_0()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Zero), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileLongLiteral_1()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.One), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileStringLiteral()
        {
            Emit(OpCodes.Ldstr, _script.GetStringLiteral(_byteReader.ReadUInt16()));
            EmitGetScriptReference();
            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(string), _scriptReference }));
            _stack++;
        }

        private void CompileTrueLiteral()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.True), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompileFalseLiteral()
        {
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.False), _publicStatic).GetMethod);
            _stack++;
        }

        private void CompilePop()
        {
            EmitGetScriptReference();
            SwapTopByRef<IScriptReference, ScriptVar>();
            Emit(OpCodes.Callvirt, _removeReference);
            _stack--;
        }

        private void CompilePop_NoRef()
        {
            Emit(OpCodes.Pop);
            _stack--;
        }

        private void CompileDup()
        {
            Emit(OpCodes.Dup);
            EmitGetScriptReference();
            SwapTopByRef<IScriptReference, ScriptVar>();
            Emit(OpCodes.Callvirt, _addReference);
            _stack++;
        }

        private void CompileDup_NoRef()
        {
            Emit(OpCodes.Dup);
            _stack++;
        }

        private void CompileAdd()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            Emit(OpCodes.Dup);
            EmitStloc(leftLocal);
            EmitLdloc(rightLocal);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Add), _publicStatic));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileSub()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            Emit(OpCodes.Dup);
            EmitStloc(leftLocal);
            EmitLdloc(rightLocal);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Sub), _publicStatic));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileMul()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            Emit(OpCodes.Dup);
            EmitStloc(leftLocal);
            EmitLdloc(rightLocal);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Mul), _publicStatic));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileDiv()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            Emit(OpCodes.Dup);
            EmitStloc(leftLocal);
            EmitLdloc(rightLocal);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Div), _publicStatic));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileMod()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            Emit(OpCodes.Dup);
            EmitStloc(leftLocal);
            EmitLdloc(rightLocal);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Mod), _publicStatic));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileAddStr()
        {
            MethodInfo toString = _scriptVar.GetMethod(nameof(ScriptVar.ToString), _publicInstance, new Type[] { typeof(IScriptReference) });
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();
            EmitStloc(rightLocal);
            EmitStloc(leftLocal);

            EmitLdloca(leftLocal);
            EmitGetScriptReference();
            Emit(OpCodes.Call, toString);

            EmitLdloca(rightLocal);
            EmitGetScriptReference();
            Emit(OpCodes.Call, toString);

            Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), _publicStatic, new Type[] { typeof(string), typeof(string) }));
            EmitGetScriptReference();
            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(string), _scriptReference }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileEq()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Equals), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptReference) }));

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileNeq()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Equals), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptReference) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileLt()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Clt);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileLte()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Cgt);
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileGt()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Cgt);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileGte()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Clt);
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileInvert()
        {
            int temp = GetTempLocal<ScriptVar>();
            int typeLocal = GetTempLocal<int>();

            Label end = _il.DefineLabel();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Bool);
            Emit(OpCodes.Beq, end);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, (int)ScriptErrorCode.R_InvalidOperation);
            Emit(OpCodes.Ldstr, "Invalid Operation");

            Emit(OpCodes.Ldstr, "Cannot invert ");
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitStloc(typeLocal);
            EmitLdloca(typeLocal);
            Emit(OpCodes.Constrained, typeof(ScriptVarType));
            Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(object.ToString), _publicInstance));
            Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), _publicStatic, new Type[] { typeof(string), typeof(string) }));

            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Error)));
            Exit();

            MarkLabel(end);

            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(temp);
            ReleaseTempLocal<int>(typeLocal);
        }

        private void CompileNeg()
        {
            int temp = GetTempLocal<ScriptVar>();
            int typeLocal = GetTempLocal<int>();

            Label end = _il.DefineLabel();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Double);
            Emit(OpCodes.Beq, end);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, (int)ScriptErrorCode.R_InvalidOperation);
            Emit(OpCodes.Ldstr, "Invalid Operation");

            Emit(OpCodes.Ldstr, "Cannot negate ");
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitStloc(typeLocal);
            EmitLdloca(typeLocal);
            Emit(OpCodes.Constrained, typeof(ScriptVarType));
            Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(object.ToString), _publicInstance));
            Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), _publicStatic, new Type[] { typeof(string), typeof(string) }));

            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Error)));
            Exit();

            MarkLabel(end);

            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            Emit(OpCodes.Neg);

            Emit(OpCodes.Newobj, _scriptVarCtorDouble);

            ReleaseTempLocal<ScriptVar>(temp);
            ReleaseTempLocal<int>(typeLocal);
        }

        private void CompileAnd()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();
            int typeLocal = GetTempLocal<int>();

            Label end = _il.DefineLabel();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Bool);
            Emit(OpCodes.Ceq);

            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Bool);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.And);
            Emit(OpCodes.Brtrue, end);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, (int)ScriptErrorCode.R_InvalidOperation);
            Emit(OpCodes.Ldstr, "Invalid Operation");
            Emit(OpCodes.Ldstr, "And operator can only be used with boolean operands.");
            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Error)));
            Exit();

            MarkLabel(end);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            Emit(OpCodes.And);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            ReleaseTempLocal<int>(typeLocal);
            _stack--;
        }

        private void CompileOr()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();
            int typeLocal = GetTempLocal<int>();

            Label end = _il.DefineLabel();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Bool);
            Emit(OpCodes.Ceq);

            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Type), _publicInstance).GetMethod);
            EmitLdc_I4((int)ScriptVarType.Bool);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.And);
            Emit(OpCodes.Brtrue, end);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, (int)ScriptErrorCode.R_InvalidOperation);
            Emit(OpCodes.Ldstr, "Invalid Operation");
            Emit(OpCodes.Ldstr, "Or operator can only be used with boolean operands.");
            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Error)));
            Exit();

            MarkLabel(end);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            Emit(OpCodes.Or);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            ReleaseTempLocal<int>(typeLocal);
            _stack--;
        }

        private void CompilePrint()
        {
            int temp = GetTempLocal<ScriptVar>();
            EmitStloc(temp);

            Emit(OpCodes.Ldarg_0);
            EmitLdloca(temp);
            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.ToString), _publicInstance, new Type[] { typeof(IScriptReference) }));
            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Print), _publicInstance, new Type[] { typeof(string) }));

            EmitGetScriptReference();
            EmitLdloca(temp);
            Emit(OpCodes.Callvirt, _removeReference);
            _stack--;
        }

        private void CompileExit()
        {
            Exit();
        }

        private void Exit()
        {
            while (--_stack > 0)
            {
                Emit(OpCodes.Pop);
            }
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Null), _publicStatic).GetMethod);
            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Return), _publicInstance));
            Emit(OpCodes.Ret);
        }

        private void CompileSetLoc()
        {
            CompileSetLoc(GetLocal(_byteReader.ReadByte()));
        }

        private void CompileSetLoc_0()
        {
            CompileSetLoc(GetLocal(0));
        }

        private void CompileSetLoc_1()
        {
            CompileSetLoc(GetLocal(1));
        }

        private void CompileSetLoc_2()
        {
            CompileSetLoc(GetLocal(2));
        }

        private void CompileSetLoc_3()
        {
            CompileSetLoc(GetLocal(3));
        }

        private void CompileSetLoc(Local local)
        {
            if (local.Type == LocalType.InVar)
            {
                Emit(OpCodes.Starg_S, local.Index + 1);
                EmitGetScriptReference();
                Emit(OpCodes.Ldarga_S, local.Index + 1);
            }
            else
            {
                EmitStloc(local.Index);
                EmitGetScriptReference();
                EmitLdloca(local.Index);
            }
            Emit(OpCodes.Callvirt, _removeReference);
            _stack--;
        }

        private void CompileLoadLoc()
        {
            CompileLoadLoc(GetLocal(_byteReader.ReadByte()));
        }

        private void CompileLoadLoc_0()
        {
            CompileLoadLoc(GetLocal(0));
        }

        private void CompileLoadLoc_1()
        {
            CompileLoadLoc(GetLocal(1));
        }

        private void CompileLoadLoc_2()
        {
            CompileLoadLoc(GetLocal(2));
        }

        private void CompileLoadLoc_3()
        {
            CompileLoadLoc(GetLocal(3));
        }

        private void CompileLoadLoc(Local local)
        {
            if (local.Type == LocalType.InVar)
            {
                if (_inVars <= 4)
                {
                    Emit(OpCodes.Ldarg_S, local.Index + 1);
                    EmitGetScriptReference();
                    Emit(OpCodes.Ldarga_S, local.Index + 1);
                }
                else
                {
                    Emit(OpCodes.Ldarg_1);
                    EmitLdc_I4(local.Index);
                    Emit(OpCodes.Ldelem, typeof(ScriptVar));
                    EmitGetScriptReference();
                    Emit(OpCodes.Ldelema, typeof(ScriptVar));
                }
            }
            else
            {
                EmitLdloc(local.Index);
                EmitGetScriptReference();
                EmitLdloca(local.Index);
            }
            Emit(OpCodes.Callvirt, _addReference);
            _stack++;
        }

        private void CompileJump()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            Emit(OpCodes.Br, label);
        }

        private void CompileJumpT()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int temp = GetTempLocal<ScriptVar>();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue)));
            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(temp);
            _stack--;
        }

        private void CompileJumpF()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int temp = GetTempLocal<ScriptVar>();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue)));
            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(temp);
            _stack--;
        }

        private void CompileJumpEq()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Equals), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptReference) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileJumpNeq()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.Equals), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptReference) }));

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileJumpLt()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Clt);

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpLte()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Cgt);

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpGt()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Cgt);

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpGte()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.CompareTo), _publicInstance, new Type[] { typeof(ScriptVar), typeof(IScriptRuntime) }));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Clt);

            EmitGetScriptReference();
            Emit(OpCodes.Dup);
            EmitLdloca(rightLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Callvirt, _removeReference);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileInvoke()
        {
            ScriptMethod method = ScriptMethod.GetMethod(_byteReader.ReadInt32());

            int[] locals = new int[method.Args.Length];
            for (int i = locals.Length - 1; i >= 0; i--)
            {
                locals[i] = GetTempLocal<ScriptVar>();
                EmitStloc(locals[i]);
            }

            int temp = GetTempLocal<ScriptVar>();

            EmitStloc(temp);
            EmitLdloca(temp);
            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));
            Emit(OpCodes.Castclass, method.DeclaringType.Type);

            ReleaseTempLocal<ScriptVar>(temp);

            if (method.RuntimeArg)
            {
                Emit(OpCodes.Ldarg_0);
            }

            for (int i = 0; i < locals.Length; i++)
            {
                if (method.Args[i].Type == typeof(ScriptVar))
                {
                    EmitLdloc(locals[i]);
                }
                else if (method.Args[i].Type == typeof(double))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(long))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetLongValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(bool))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(string))
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetStringValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(IScriptObject))
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));
                }
                else
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));
                    Emit(OpCodes.Castclass, method.Args[i].Type);
                }
            }

            for (int i = locals.Length - 1; i >= 0; i--)
            {
                ReleaseTempLocal<ScriptVar>(locals[i]);
            }

            Emit(OpCodes.Callvirt, method.Method);

            _stack -= method.Args.Length + 1;

            if (method.Method.ReturnType == typeof(ScriptVar))
            {
                return;
            }
            else if (method.Method.ReturnType == typeof(void))
            {
                Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Null), _publicStatic).GetMethod);
            }
            else if (method.Method.ReturnType == typeof(double))
            {
                Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            }
            else if (method.Method.ReturnType == typeof(long))
            {
                Emit(OpCodes.Conv_R8);
                Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            }
            else if (method.Method.ReturnType == typeof(bool))
            {
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));
            }
            else if (method.Method.ReturnType == typeof(string))
            {
                EmitGetScriptReference();
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(string), typeof(IScriptReference) }));
            }
            else
            {
                EmitGetScriptReference();
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(IScriptObject), typeof(IScriptReference) }));
            }
        }

        private void CompileInvokeDynamic() {}

        private void CompileInvokeStatic()
        {
            ScriptMethod method = ScriptMethod.GetMethod(_byteReader.ReadInt32());

            int[] locals = new int[method.Args.Length];
            for (int i = locals.Length - 1; i >= 0; i--)
            {
                locals[i] = GetTempLocal<ScriptVar>();
                EmitStloc(locals[i]);
            }

            if (method.RuntimeArg)
            {
                Emit(OpCodes.Ldarg_0);
            }

            for (int i = 0; i < locals.Length; i++)
            {
                if (method.Args[i].Type == typeof(ScriptVar))
                {
                    EmitLdloc(locals[i]);
                }
                else if (method.Args[i].Type == typeof(double))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(long))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetLongValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(bool))
                {
                    EmitLdloca(locals[i]);
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(string))
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetStringValue), _publicInstance));
                }
                else if (method.Args[i].Type == typeof(IScriptObject))
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));
                }
                else
                {
                    EmitLdloca(locals[i]);
                    EmitGetScriptReference();
                    Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));
                    Emit(OpCodes.Castclass, method.Args[i].Type);
                }
            }

            for (int i = locals.Length - 1; i >= 0; i--)
            {
                ReleaseTempLocal<ScriptVar>(locals[i]);
            }

            Emit(OpCodes.Call, method.Method);

            _stack -= method.Args.Length;

            if (method.Method.ReturnType == typeof(ScriptVar))
            {
                return;
            }
            else if (method.Method.ReturnType == typeof(void))
            {
                Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Null), _publicStatic).GetMethod);
            }
            else if (method.Method.ReturnType == typeof(double))
            {
                Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            }
            else if (method.Method.ReturnType == typeof(long))
            {
                Emit(OpCodes.Conv_R8);
                Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            }
            else if (method.Method.ReturnType == typeof(bool))
            {
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));
            }
            else if (method.Method.ReturnType == typeof(string))
            {
                EmitGetScriptReference();
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(string), typeof(IScriptReference) }));
            }
            else
            {
                EmitGetScriptReference();
                Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(IScriptObject), typeof(IScriptReference) }));
            }
        }

        private void CompileGetProperty()
        {
            int temp = GetTempLocal<ScriptVar>();
            ScriptProperty property = ScriptProperty.GetProperty(_byteReader.ReadInt32());

            EmitStloc(temp);
            Emit(OpCodes.Ldc_I4, property.Id);
            Emit(OpCodes.Call, typeof(ScriptProperty).GetMethod(nameof(ScriptProperty.GetProperty), _publicStatic, new Type[] { typeof(int) }));

            EmitLdloca(temp);
            EmitGetScriptReference();
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue), _publicInstance));

            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, typeof(ScriptProperty).GetMethod(nameof(ScriptProperty.Get), _publicInstance));

            if (property.PropertyType.MaybeRef)
            {

                EmitStloc(temp);
                EmitGetScriptReference();
                EmitLdloca(temp);
                Emit(OpCodes.Callvirt, _addReference);
                EmitLdloc(temp);
            }

            ReleaseTempLocal<ScriptVar>(temp);
        }

        private void CompileSetProperty()
        {
            throw new NotImplementedException();
        }

        private void CompileSetDynamicProperty()
        {
            throw new NotImplementedException();
        }

        private void CompileGetStaticProperty()
        {
            ScriptProperty property = ScriptProperty.GetProperty(_byteReader.ReadInt32());

            Emit(OpCodes.Ldc_I4, property.Id);
            Emit(OpCodes.Call, typeof(ScriptProperty).GetMethod(nameof(ScriptProperty.GetProperty), _publicStatic, new Type[] { typeof(int) }));

            Emit(OpCodes.Ldnull);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Call, typeof(ScriptProperty).GetMethod(nameof(ScriptProperty.Get), _publicInstance));

            if (property.PropertyType.MaybeRef)
            {
                int temp = GetTempLocal<ScriptVar>();

                EmitStloc(temp);
                EmitGetScriptReference();
                EmitLdloca(temp);
                Emit(OpCodes.Callvirt, _addReference);
                EmitLdloc(temp);

                ReleaseTempLocal<ScriptVar>(temp);
            }

            _stack++;
        }

        private void CompileSetStaticProperty()
        {
            throw new NotImplementedException();
        }

        private void CompileCheckType()
        {
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, _byteReader.ReadInt32());
            Emit(OpCodes.Call, typeof(ScriptCILCompiler).GetMethod(nameof(CheckType), BindingFlags.NonPublic | BindingFlags.Static));
            _stack--;
        }

        private void CompilePeekCheckType()
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldc_I4, _byteReader.ReadInt32());
            Emit(OpCodes.Call, typeof(ScriptCILCompiler).GetMethod(nameof(CheckType), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void CheckType(ScriptVar left, IScriptRuntime runtime, int right)
        {
            if (right == ScriptType.ScriptVar.Id)
            {
                return;
            }

            ScriptType type = ScriptType.GetType(right);
            if (left.Type == ScriptVarType.Null)
            {
                if (type.Nullable)
                {
                    return;
                }
                else
                {
                    runtime.TypeError(null, new string[] { type.Name }, "null");
                }
            }

            ScriptType leftType = left.GetScriptType(runtime.Reference);
            if (!leftType.IsTypeOrSubType(type))
            {
                runtime.TypeError(null, new string[] { type.Name }, leftType);
            }
        }

        private void CompileLoop()
        {
            throw new NotImplementedException();
        }

        private void CompileLoop_Num()
        {
            throw new NotImplementedException();
        }

        private void CompileAdd_Num()
        {
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.AddDouble), _publicStatic));
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack--;
        }

        private void CompileSub_Num()
        {
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.SubDouble), _publicStatic));
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack--;
        }

        private void CompileMul_Num()
        {
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.MulDouble), _publicStatic));
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack--;
        }

        private void CompileDiv_Num()
        {
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.DivDouble), _publicStatic));
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack--;
        }

        private void CompileMod_Num()
        {
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.ModDouble), _publicStatic));
            Emit(OpCodes.Newobj, _scriptVarCtorDouble);
            _stack--;
        }

        private void CompileLt_Num()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Clt);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileLte_Num()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Cgt);
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileGt_Num()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Cgt);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileGte_Num()
        {
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Clt);
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileNeg_Num()
        {
            int temp = GetTempLocal<ScriptVar>();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            Emit(OpCodes.Neg);

            Emit(OpCodes.Newobj, _scriptVarCtorDouble);

            ReleaseTempLocal<ScriptVar>(temp);
        }

        private void CompileJumpEq_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            EmitLdloc(rightLocal);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileJumpNeq_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);

            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack--;
        }

        private void CompileJumpLt_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Clt);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpLte_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Cgt);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpGt_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Cgt);

            Emit(OpCodes.Brtrue, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileJumpGte_Num()
        {
            Label label = GetLabel(_byteReader.ReadInt32());
            int rightLocal = GetTempLocal<ScriptVar>();
            int leftLocal = GetTempLocal<ScriptVar>();

            EmitStloc(rightLocal);
            EmitStloc(leftLocal);
            EmitLdloca(leftLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));
            EmitLdloca(rightLocal);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue), _publicInstance));

            Emit(OpCodes.Clt);

            Emit(OpCodes.Brfalse, label);

            ReleaseTempLocal<ScriptVar>(rightLocal);
            ReleaseTempLocal<ScriptVar>(leftLocal);
            _stack -= 2;
        }

        private void CompileSetLoc_NoRef()
        {
            CompileSetLoc_NoRef(GetLocal(_byteReader.ReadByte()));
        }

        private void CompileSetLoc_0_NoRef()
        {
            CompileSetLoc_NoRef(GetLocal(0));
        }

        private void CompileSetLoc_1_NoRef()
        {
            CompileSetLoc_NoRef(GetLocal(1));
        }

        private void CompileSetLoc_2_NoRef()
        {
            CompileSetLoc_NoRef(GetLocal(2));
        }

        private void CompileSetLoc_3_NoRef()
        {
            CompileSetLoc_NoRef(GetLocal(3));
        }

        private void CompileSetLoc_NoRef(Local local)
        {
            if (local.Type == LocalType.InVar)
            {
                Emit(OpCodes.Starg_S, local.Index + 1);
            }
            else
            {
                EmitStloc(local.Index);
            }
            _stack--;
        }

        private void CompileLoadLoc_NoRef()
        {
            CompileLoadLoc_NoRef(GetLocal(_byteReader.ReadByte()));
        }

        private void CompileLoadLoc_0_NoRef()
        {
            CompileLoadLoc_NoRef(GetLocal(0));
        }

        private void CompileLoadLoc_1_NoRef()
        {
            CompileLoadLoc_NoRef(GetLocal(1));
        }

        private void CompileLoadLoc_2_NoRef()
        {
            CompileLoadLoc_NoRef(GetLocal(2));
        }

        private void CompileLoadLoc_3_NoRef()
        {
            CompileLoadLoc_NoRef(GetLocal(3));
        }

        private void CompileLoadLoc_NoRef(Local local)
        {
            if (local.Type == LocalType.InVar)
            {
                if (_inVars <= 4)
                {
                    Emit(OpCodes.Ldarg_S, local.Index + 1);
                }
                else
                {
                    Emit(OpCodes.Ldarg_1);
                    EmitLdc_I4(local.Index);
                    Emit(OpCodes.Ldelem, typeof(ScriptVar));
                }
            }
            else
            {
                EmitLdloc(local.Index);
            }
            _stack++;
        }

        private void CompileInvert_Bool()
        {
            int temp = GetTempLocal<ScriptVar>();

            EmitStloc(temp);
            EmitLdloca(temp);
            Emit(OpCodes.Call, _scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue), _publicInstance));
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);

            Emit(OpCodes.Newobj, _scriptVar.GetConstructor(new Type[] { typeof(bool) }));

            ReleaseTempLocal<ScriptVar>(temp);
        }

        private void CompileReturn()
        {
            if (_stack == 0)
            {
                Emit(OpCodes.Ldarg_0);
                Emit(OpCodes.Call, _scriptVar.GetProperty(nameof(ScriptVar.Null), _publicStatic).GetMethod);
            }
            else
            {
                while (--_stack > 1)
                {
                    Emit(OpCodes.Pop);
                }
                Emit(OpCodes.Ldarg_0);
                Swap<IScriptRuntime, ScriptVar>();
            }
            Emit(OpCodes.Callvirt, _scriptRuntime.GetMethod(nameof(IScriptRuntime.Return), _publicInstance));
            Emit(OpCodes.Ret);
        }

        private Local GetLocal(int index)
        {
            short inVar = _inVarLookup[index];
            if (inVar != -1)
            {
                return new Local((byte)inVar, LocalType.InVar);
            }
            else
            {
                // Scripts typically use the locals to store InVars,
                // but CIL scripts use parameters instead. So we
                // subtract the total number of InVars from all of
                // the local indices to not have several unused locals.
                return new Local((byte)(index - _inVars), LocalType.Local);
            }
        }

        private Label GetLabel(int position)
        {
            foreach (ScriptLabel label in _labels)
            {
                if (label.Position == position)
                {
                    return label.Label;
                }
            }
            throw new InvalidOperationException($"Invalid label {position}, this shouldn't be possible");
        }

        public string GetCILOutputString()
        {
            return _stringBuilder.ToString();
        }

        public ScriptCILCompiler()
        {
            _byteReader = new UnsafeByteReader();
            _inVarLookup = new short[4];
            _labels = new List<ScriptLabel>();
            _tempLocals = new Dictionary<Type, Stack<int>>();
            _locals = new List<LocalBuilder>();
            _commands = new Action[]
            {
                CompileNop,
                CompileNullLiteral,
                CompileDoubleLiteral,
                CompileDoubleLiteral_0,
                CompileDoubleLiteral_1,
                CompileLongLiteral,
                CompileLongLiteral_0,
                CompileLongLiteral_1,
                CompileStringLiteral,
                CompileTrueLiteral,
                CompileFalseLiteral,
                CompilePop,
                CompilePop_NoRef,
                CompileDup,
                CompileDup_NoRef,
                CompileAdd,
                CompileSub,
                CompileMul,
                CompileDiv,
                CompileMod,
                CompileAddStr,
                CompileEq,
                CompileNeq,
                CompileLt,
                CompileLte,
                CompileGt,
                CompileGte,
                CompileInvert,
                CompileNeg,
                CompileAnd,
                CompileOr,
                CompilePrint,
                CompileExit,
                CompileSetLoc,
                CompileSetLoc_0,
                CompileSetLoc_1,
                CompileSetLoc_2,
                CompileSetLoc_3,
                CompileLoadLoc,
                CompileLoadLoc_0,
                CompileLoadLoc_1,
                CompileLoadLoc_2,
                CompileLoadLoc_3,
                CompileJump,
                CompileJumpT,
                CompileJumpF,
                CompileJumpEq,
                CompileJumpNeq,
                CompileJumpLt,
                CompileJumpLte,
                CompileJumpGt,
                CompileJumpGte,
                CompileInvoke,
                CompileInvokeDynamic,
                CompileInvokeStatic,
                CompileGetProperty,
                CompileSetProperty,
                CompileSetDynamicProperty,
                CompileGetStaticProperty,
                CompileSetStaticProperty,
                CompileCheckType,
                CompilePeekCheckType,
                CompileLoop,
                CompileLoop_Num,
                CompileAdd_Num,
                CompileSub_Num,
                CompileMul_Num,
                CompileDiv_Num,
                CompileMod_Num,
                CompileLt_Num,
                CompileLte_Num,
                CompileGt_Num,
                CompileGte_Num,
                CompileNeg_Num,
                CompileJumpEq_Num,
                CompileJumpNeq_Num,
                CompileJumpLt_Num,
                CompileJumpLte_Num,
                CompileJumpGt_Num,
                CompileJumpGte_Num,
                CompileSetLoc_NoRef,
                CompileSetLoc_0_NoRef,
                CompileSetLoc_1_NoRef,
                CompileSetLoc_2_NoRef,
                CompileSetLoc_3_NoRef,
                CompileLoadLoc_NoRef,
                CompileLoadLoc_0_NoRef,
                CompileLoadLoc_1_NoRef,
                CompileLoadLoc_2_NoRef,
                CompileLoadLoc_3_NoRef,
                CompileInvert_Bool,
                CompileReturn
            };
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _byteReader.Dispose();
                }

                _byteReader = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
