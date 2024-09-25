using DaveTheMonitor.Scripts.Runtime;
using System;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts
{
    public unsafe sealed partial class ScriptRuntime : IScriptRuntime
    {
        public ScriptRuntimeType RuntimeType { get; private set; }
        public IScriptReference Reference => _reference;
        public ScriptVar ReturnedValue { get; private set; }
        public object ReturnedObject { get; private set; }
        public event ScriptErrorEventHandler ErrorHandler;
        public event ScriptPrintEventHandler PrintHandler;
        private Script _currentScript;
        private ScriptVar[] _locals;
        private ScriptReference _reference;
        private List<ScriptVar> _invokeArgs;
        private bool _exited;
        private bool _disposedValue;
        private int _usedLocals;

        #region Ops

        static ScriptRuntime()
        {
            InitOps();
        }

        private static void Nop(ScriptRuntime runtime)
        {

        }

        #region Literals

        private static void NullLiteral(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.Null);
        }

        private static void DoubleLiteral(ScriptRuntime runtime)
        {
            runtime.Push(new ScriptVar(runtime.ReadDouble()));
        }

        private static void DoubleLiteral_0(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.Zero);
        }

        private static void DoubleLiteral_1(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.One);
        }

        private static void LongLiteral(ScriptRuntime runtime)
        {
            ScriptVar v = new ScriptVar(runtime.ReadInt64());
            runtime.Push(v);
        }

        private static void LongLiteral_0(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.Zero);
        }

        private static void LongLiteral_1(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.One);
        }

        private static void StringLiteral(ScriptRuntime runtime)
        {
            runtime.Push(new ScriptVar(runtime._currentScript.GetStringLiteral(runtime.ReadUInt16()), runtime._reference));
        }

        private static void TrueLiteral(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.True);
        }

        private static void FalseLiteral(ScriptRuntime runtime)
        {
            runtime.Push(ScriptVar.False);
        }

        #endregion

        #region Operations

        private static void Pop(ScriptRuntime runtime)
        {
            runtime._reference.RemoveReference(runtime.Pop());
        }

        private static void Pop_NoRef(ScriptRuntime runtime)
        {
            runtime.Pop();
        }

        private static void Dup(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Peek();
            runtime.Push(v);
            runtime._reference.AddReference(ref v);
        }

        private static void Dup_NoRef(ScriptRuntime runtime)
        {
            runtime.Push(runtime.Peek());
        }

        private static void Add(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(ScriptVar.Add(left, right, runtime));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Sub(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.Sub(left, right, runtime)));
        }

        private static void Mul(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.Mul(left, right, runtime)));
        }

        private static void Div(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.Div(left, right, runtime)));
        }

        private static void Mod(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.Mod(left, right, runtime)));
        }

        private static void AddStr(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            string v = left.ToString(runtime._reference) + right.ToString(runtime._reference);
            int r = runtime._reference.AddReference(v);
            runtime.Push(ScriptVar.CreateString(r));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Eq(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.Equals(right, runtime._reference)));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Neq(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(!left.Equals(right, runtime._reference)));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Lt(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.CompareTo(right, runtime) < 0));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Lte(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.CompareTo(right, runtime) <= 0));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Gt(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.CompareTo(right, runtime) > 0));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Gte(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.CompareTo(right, runtime) >= 0));
            runtime._reference.RemoveReference(left);
            runtime._reference.RemoveReference(right);
        }

        private static void Invert(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Pop();
            if (v.Type == ScriptVarType.Bool)
            {
                runtime.Push(new ScriptVar(!v.GetBoolValue()));
                return;
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperation, "Invalid Operation", $"Cannot invert {v.Type}");
        }

        private static void Neg(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Pop();
            if (v.Type == ScriptVarType.Double)
            {
                runtime.Push(new ScriptVar(-v.GetDoubleValue()));
                return;
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperation, "Invalid Operation", $"Cannot negate {v.Type}");
        }

        private static void And(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.Type == ScriptVarType.Bool && right.Type == ScriptVarType.Bool)
            {
                runtime.Push(new ScriptVar(left.GetBoolValue() && right.GetBoolValue()));
                return;
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperation, "Invalid Operation", "And operator can only be used with boolean operands.");
        }

        private static void Or(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.Type == ScriptVarType.Bool && right.Type == ScriptVarType.Bool)
            {
                runtime.Push(new ScriptVar(left.GetBoolValue() || right.GetBoolValue()));
                return;
            }
            runtime.Error(ScriptErrorCode.R_InvalidOperation, "Invalid Operation", "Or operator can only be used with boolean operands.");
        }

        private static void Print(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Pop();
            runtime.PrintHandler?.Invoke(runtime, new ScriptPrintEventArgs(v.ToString(runtime._reference)));
            runtime._reference.RemoveReference(v);
        }

        private static void Exit(ScriptRuntime runtime)
        {
            runtime.ReturnedValue = ScriptVar.Null;
            runtime.ReturnedObject = null;
            runtime._exited = true;
        }

        #endregion

        #region Locals

        private static void SetLoc(ScriptRuntime runtime)
        {
            int index = runtime.ReadByte();
            runtime.SetLoc(index);
        }

        private static void SetLoc_0(ScriptRuntime runtime)
        {
            runtime.SetLoc(0);
        }

        private static void SetLoc_1(ScriptRuntime runtime)
        {
            runtime.SetLoc(1);
        }

        private static void SetLoc_2(ScriptRuntime runtime)
        {
            runtime.SetLoc(2);
        }

        private static void SetLoc_3(ScriptRuntime runtime)
        {
            runtime.SetLoc(3);
        }

        private void SetLoc(int local)
        {
            _reference.RemoveReference(_locals[local]);
            _locals[local] = Pop();
            _usedLocals = Math.Max(_usedLocals, local + 1);
        }

        private static void LoadLoc(ScriptRuntime runtime)
        {
            int index = runtime.ReadByte();
            runtime.LoadLoc(index);
        }

        private static void LoadLoc_0(ScriptRuntime runtime)
        {
            runtime.LoadLoc(0);
        }

        private static void LoadLoc_1(ScriptRuntime runtime)
        {
            runtime.LoadLoc(1);
        }

        private static void LoadLoc_2(ScriptRuntime runtime)
        {
            runtime.LoadLoc(2);
        }

        private static void LoadLoc_3(ScriptRuntime runtime)
        {
            runtime.LoadLoc(3);
        }

        private void LoadLoc(int local)
        {
            ScriptVar v = _locals[local];
            _reference.AddReference(ref v);
            Push(v);
        }

        #endregion

        #region Jumps

        private static void Jump(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.Jump(pos);
        }

        private static void JumpT(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            ScriptVar v = runtime.Pop();
            if (v.GetBoolValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpF(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            ScriptVar v = runtime.Pop();
            if (!v.GetBoolValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpEq(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.Equals(right, runtime._reference))
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpNeq(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (!left.Equals(right, runtime._reference))
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpLt(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.CompareTo(right, runtime) < 0)
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpLte(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.CompareTo(right, runtime) <= 0)
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpGt(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.CompareTo(right, runtime) > 0)
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpGte(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.CompareTo(right, runtime) >= 0)
            {
                runtime.Jump(pos);
            }
        }

        private void Jump(int pos)
        {
            ProgramPosition = pos;
        }

        #endregion

        #region Invoke/Properties

        private static void Invoke(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptMethod method = ScriptMethod.GetMethod(id);
            runtime.PrepareArgs(method.Args.Length);

            ScriptVar objVar = runtime.Pop();
            IScriptObject obj = objVar.GetObjectValue(runtime._reference);

            List<ScriptVar> invoke = runtime._invokeArgs;
            ScriptVar result = method.Invoke(obj, runtime, invoke);
            runtime._reference.RemoveReference(objVar);
            foreach (ScriptVar arg in invoke)
            {
                runtime._reference.RemoveReference(arg);
            }

            runtime.Push(result);
            invoke.Clear();
        }

        private static void InvokeDynamic(ScriptRuntime runtime)
        {
            string name = runtime._currentScript.GetStringLiteral(runtime.ReadUInt16());
            int args = runtime.ReadUInt16();
            runtime.PrepareArgs(args);

            ScriptVar objVar = runtime.Pop();
            if (objVar.Type != ScriptVarType.Object)
            {
                runtime.Error(ScriptErrorCode.R_InvalidMemberInvoke, "Invalid Member Invoke", $"Cannot invoke member of {objVar.Type}");
                return;
            }
            IScriptObject obj = objVar.GetObjectValue(runtime._reference);

            ScriptMethod method = obj.ScriptType.GetMethod(name);
            if (method == null)
            {
                ScriptProperty property = obj.ScriptType.GetProperty(name);
                if (property == null)
                {
                    runtime.Error(ScriptErrorCode.R_InvalidMember, "Invalid Member", $"Member {name} does not exist on {obj.ScriptType.Name}.");
                    return;
                }
                else if (!property.CanGet)
                {
                    runtime.Error(ScriptErrorCode.R_NoPropertyGetter, "Invalid Property", $"Property {property.Name} on {obj.ScriptType.Name} does not have a getter.");
                    return;
                }

                ScriptVar propResult = property.Get(obj, runtime);
                runtime.Push(propResult);
                return;
            }

            runtime.CheckArgs(method.Args);
            List<ScriptVar> invoke = runtime._invokeArgs;
            ScriptVar result = method.Invoke(obj, runtime, invoke);
            runtime._reference.RemoveReference(objVar);
            foreach (ScriptVar arg in invoke)
            {
                runtime._reference.RemoveReference(arg);
            }

            runtime.Push(result);
            invoke.Clear();
        }

        private static void InvokeStatic(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptMethod method = ScriptMethod.GetMethod(id);
            runtime.PrepareArgs(method.Args.Length);

            List<ScriptVar> invoke = runtime._invokeArgs;
            ScriptVar result = method.Invoke(null, runtime, invoke);
            foreach (ScriptVar arg in invoke)
            {
                runtime._reference.RemoveReference(arg);
            }

            runtime.Push(result);
            invoke.Clear();
        }

        private void PrepareArgs(int count)
        {
            if (count == 0)
            {
                return;
            }

            // This is much faster than inserting at the start of the list
            // and doesn't scale terribly for methods that take > 1 arg,
            // but slightly slower for methods that take only 1 arg
            // TODO: reverse access in ScriptMethod invoker?
            List<ScriptVar> invoke = _invokeArgs;
            invoke.EnsureCapacity(count);
#if NET8_0_OR_GREATER
            CollectionsMarshal.SetCount(invoke, count);
#else
            for (int i = 0; i < count; i++)
            {
                invoke.Add(ScriptVar.Null);
            }
#endif
            for (int i = count - 1; i >= 0; i--)
            {
                invoke[i] = Pop();
            }
        }

        private void CheckArgs(ScriptType[] types)
        {
            List<ScriptVar> args = _invokeArgs;
            if (types.Length != args.Count)
            {
                Error(ScriptErrorCode.R_ArgCountError, "Invoke Error", $"Invalid argument count passed to method invoke. Expected {types.Length} args, received {args.Count}");
                return;
            }

            for (int i = 0; i < args.Count; i++)
            {
                ScriptType t = types[i];
                if (t.Id != 1 && args[i].GetScriptType(Reference) != t)
                {
                    Error(ScriptErrorCode.R_ArgTypeError, "Invoke Error", $"Invalid argument type passed to method invoke. Expected {types[i].Name}, received {args[i].GetScriptType(_reference).Name}");
                    return;
                }
            }
        }

        private static void GetProperty(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptVar objVar = runtime.Pop();
            IScriptObject obj = objVar.GetObjectValue(runtime._reference);
            runtime._reference.RemoveReference(objVar);
            ScriptProperty property = ScriptProperty.GetProperty(id);

            ScriptVar result = property.Get(obj, runtime);
            runtime.Push(result);
        }

        private static void SetProperty(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptVar value = runtime.Pop();
            ScriptVar objVar = runtime.Pop();
            IScriptObject obj = objVar.GetObjectValue(runtime._reference);
            ScriptProperty property = ScriptProperty.GetProperty(id);

            property.Set(obj, runtime, value);
            runtime._reference.RemoveReference(objVar);
            runtime._reference.RemoveReference(value);
        }

        private static void SetDynamicProperty(ScriptRuntime runtime)
        {
            string name = runtime._currentScript.GetStringLiteral(runtime.ReadUInt16());

            ScriptVar value = runtime.Pop();
            ScriptVar objVar = runtime.Pop();
            IScriptObject obj = objVar.GetObjectValue(runtime._reference);

            ScriptProperty property = obj.ScriptType.GetProperty(name);
            if (property == null)
            {
                runtime.Error(ScriptErrorCode.R_InvalidMember, "Invalid Property", $"Property {property.Name} does not exist on {obj.ScriptType.Name}.");
                return;
            }
            else if (!property.CanSet)
            {
                runtime.Error(ScriptErrorCode.R_NoPropertySetter, "Invalid Property", $"Property {property.Name} on {obj.ScriptType.Name} does not have a setter.");
                return;
            }

            property.Set(obj, runtime, value);
            runtime._reference.RemoveReference(objVar);
            runtime._reference.RemoveReference(value);
        }

        private static void GetStaticProperty(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptProperty property = ScriptProperty.GetProperty(id);

            ScriptVar result = property.Get(null, runtime);
            runtime.Push(result);
            runtime._reference.AddReference(ref result);
        }

        private static void SetStaticProperty(ScriptRuntime runtime)
        {
            int id = runtime.ReadInt32();

            ScriptVar value = runtime.Pop();
            ScriptProperty property = ScriptProperty.GetProperty(id);

            property.Set(null, runtime, value);
        }

#endregion

        private static void CheckType(ScriptRuntime runtime)
        {
            int type = runtime.ReadInt32();
            if (type != 1)
            {
                ScriptVar v = runtime.Peek();
                ScriptType t = v.GetScriptType(runtime._reference);
                runtime._reference.RemoveReference(v);
                if (!t.IsTypeOrSubType(ScriptType.GetType(type)))
                {
                    ((IScriptRuntime)runtime).TypeError(null, new string[] { ScriptType.GetType(type).Name }, t);
                }
            }
        }

        private static void PeekCheckType(ScriptRuntime runtime)
        {
            int type = runtime.ReadInt32();
            if (type != 1)
            {
                ScriptVar v = runtime.Peek();
                if (v.Type == ScriptVarType.Null)
                {
                    if (ScriptType.GetType(type).Nullable)
                    {
                        return;
                    }
                    else
                    {
                        ((IScriptRuntime)runtime).TypeError(null, new string[] { ScriptType.GetType(type).Name }, "null");
                    }
                }
                ScriptType t = v.GetScriptType(runtime._reference);

                if (!t.IsTypeOrSubType(ScriptType.GetType(type)))
                {
                    ((IScriptRuntime)runtime).TypeError(null, new string[] { ScriptType.GetType(type).Name }, t);
                }
            }
        }

        private static void Loop(ScriptRuntime runtime)
        {
            runtime.Loop(runtime.ReadInt32(), runtime.ReadInt32());
        }

        private static void Loop_Num(ScriptRuntime runtime)
        {
            runtime.Loop(runtime.ReadInt32(), (int)runtime.Pop().GetLongValue());
        }

        private void Loop(int count, int length)
        {
            int start = ProgramPosition;
            int end = start + length;
            int current = 0;
            while (current < count && !_exited)
            {
                if (ProgramPosition >= end)
                {
                    current++;
                    ProgramPosition = start;
                    continue;
                }

                byte op = ReadByte();
                CallOp((ScriptOp)op);
            }
            ProgramPosition = end;
        }

        #region TypeOps

        private static void Add_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.AddDouble(left, right)));
        }

        private static void Sub_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.SubDouble(left, right)));
        }

        private static void Mul_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.MulDouble(left, right)));
        }

        private static void Div_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.DivDouble(left, right)));
        }

        private static void Mod_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(ScriptVar.ModDouble(left, right)));
        }

        private static void Lt_Num(ScriptRuntime runtime)
        {
            // wtf calling PopLeftRight makes this method slower only when profiling
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.GetDoubleValue() < right.GetDoubleValue()));
        }

        private static void Lte_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.GetDoubleValue() <= right.GetDoubleValue()));
        }

        private static void Gt_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.GetDoubleValue() > right.GetDoubleValue()));
        }

        private static void Gte_Num(ScriptRuntime runtime)
        {
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            runtime.Push(new ScriptVar(left.GetDoubleValue() >= right.GetDoubleValue()));
        }

        private static void Neg_Num(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Pop();
            runtime.Push(new ScriptVar(-v.GetDoubleValue()));
        }

        private static void JumpEq_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() == right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpNeq_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() != right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpLt_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() < right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpLte_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() <= right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpGt_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() > right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void JumpGte_Num(ScriptRuntime runtime)
        {
            int pos = runtime.ReadInt32();
            runtime.PopLeftRight(out ScriptVar left, out ScriptVar right);
            if (left.GetDoubleValue() >= right.GetDoubleValue())
            {
                runtime.Jump(pos);
            }
        }

        private static void SetLoc_NoRef(ScriptRuntime runtime)
        {
            int index = runtime.ReadByte();
            runtime.SetLoc_NoRef(index);
        }

        private static void SetLoc_0_NoRef(ScriptRuntime runtime)
        {
            runtime.SetLoc_NoRef(0);
        }

        private static void SetLoc_1_NoRef(ScriptRuntime runtime)
        {
            runtime.SetLoc_NoRef(1);
        }

        private static void SetLoc_2_NoRef(ScriptRuntime runtime)
        {
            runtime.SetLoc_NoRef(2);
        }

        private static void SetLoc_3_NoRef(ScriptRuntime runtime)
        {
            runtime.SetLoc_NoRef(3);
        }

        private void SetLoc_NoRef(int local)
        {
            _locals[local] = Pop();
            _usedLocals = Math.Max(_usedLocals, local + 1);
        }

        private static void LoadLoc_NoRef(ScriptRuntime runtime)
        {
            int index = runtime.ReadByte();
            runtime.LoadLoc_NoRef(index);
        }

        private static void LoadLoc_0_NoRef(ScriptRuntime runtime)
        {
            runtime.LoadLoc_NoRef(0);
        }

        private static void LoadLoc_1_NoRef(ScriptRuntime runtime)
        {
            runtime.LoadLoc_NoRef(1);
        }

        private static void LoadLoc_2_NoRef(ScriptRuntime runtime)
        {
            runtime.LoadLoc_NoRef(2);
        }

        private static void LoadLoc_3_NoRef(ScriptRuntime runtime)
        {
            runtime.LoadLoc_NoRef(3);
        }

        private void LoadLoc_NoRef(int local)
        {
            Push(_locals[local]);
        }

        private static void Invert_Bool(ScriptRuntime runtime)
        {
            ScriptVar v = runtime.Pop();
            runtime.Push(new ScriptVar(!v.GetBoolValue()));
        }

        #endregion

        private static void Return(ScriptRuntime runtime)
        {
            if (runtime.StackCount > 0)
            {
                runtime.Return(runtime.Pop());
            }
            else
            {
                runtime.Return(ScriptVar.Null);
            }
        }

        public void Return(ScriptVar v)
        {
            ReturnedValue = v;
            ReturnedObject = v.Type switch
            {
                ScriptVarType.String => v.GetStringValue(Reference),
                ScriptVarType.Object => v.GetObjectValue(Reference),
                _ => null
            };
            _exited = true;
        }

#endregion

        private void InitializeScript(Script script)
        {
            if (script.CILInvoker != null)
            {
                InitializeForCILScript(script);
                return;
            }

            _currentScript = script;
            RuntimeType = script.Type;
            ReturnedValue = ScriptVar.Null;
            ClearStack();
            _reference.ClearReferences();
            SetBytes(script.Bytecode);
            _usedLocals = 0;
            _exited = false;
            if (script.InVars > 0)
            {
                Array.Fill(_locals, ScriptVar.Null, 0, script.InVars);
            }
        }

        public void InitializeForCILScript(Script script)
        {
            _currentScript = script;
            ReturnedValue = ScriptVar.Null;
            _reference.ClearReferences();
            _exited = false;
            if (script.InVars > 0)
            {
                Array.Fill(_locals, ScriptVar.Null, 0, script.InVars);
            }
        }

        public void SetInVar(string name, double value)
        {
            if (_currentScript.TryGetInVar(name, out ScriptInVarDefinition def))
            {
                if (def.Type.Id != ScriptType.ScriptVar.Id && def.Type.Id != ScriptType.Double.Id)
                {
                    Error(ScriptErrorCode.R_InVarTypeError, "Invalid InVar", $"InVar {name} is defined in script with type {def.Type.Name}, received {ScriptType.Double.Name}.");
                    return;
                }
                _locals[def.LocalIndex] = new ScriptVar(value);
            }
        }

        public void SetInVar(string name, int value)
        {
            if (_currentScript.TryGetInVar(name, out ScriptInVarDefinition def))
            {
                if (def.Type.Id != ScriptType.ScriptVar.Id && def.Type.Id != ScriptType.Long.Id)
                {
                    Error(ScriptErrorCode.R_InVarTypeError, "Invalid InVar", $"InVar {name} is defined in script with type {def.Type.Name}, received {ScriptType.Long.Name}.");
                    return;
                }
                _locals[def.LocalIndex] = new ScriptVar(value);
            }
        }

        public void SetInVar(string name, bool value)
        {
            if (_currentScript.TryGetInVar(name, out ScriptInVarDefinition def))
            {
                if (def.Type.Id != ScriptType.ScriptVar.Id && def.Type.Id != ScriptType.Bool.Id)
                {
                    Error(ScriptErrorCode.R_InVarTypeError, "Invalid InVar", $"InVar {name} is defined in script with type {def.Type.Name}, received {ScriptType.Bool.Name}.");
                    return;
                }
                _locals[def.LocalIndex] = new ScriptVar(value);
            }
        }

        public void SetInVar(string name, string value)
        {
            if (_currentScript.TryGetInVar(name, out ScriptInVarDefinition def))
            {
                if (def.Type.Id != ScriptType.ScriptVar.Id && def.Type.Id != ScriptType.String.Id)
                {
                    Error(ScriptErrorCode.R_InVarTypeError, "Invalid InVar", $"InVar {name} is defined in script with type {def.Type.Name}, received {ScriptType.String.Name}.");
                    return;
                }

                _locals[def.LocalIndex] = new ScriptVar(value, _reference);
            }
        }

        public void SetInVar(string name, IScriptObject value)
        {
            if (_currentScript.TryGetInVar(name, out ScriptInVarDefinition def))
            {
                if (def.Type.Id != ScriptType.ScriptVar.Id && !value.ScriptType.IsTypeOrSubType(def.Type))
                {
                    Error(ScriptErrorCode.R_InVarTypeError, "Invalid InVar", $"InVar {name} is defined in script with type {def.Type.Name}, received {value.ScriptType.Name}.");
                    return;
                }

                _locals[def.LocalIndex] = new ScriptVar(value, _reference);
            }
        }

        public void SetInVar(ScriptInVar inVar)
        {
            switch (inVar.Type)
            {
                case ScriptVarType.Double: SetInVar(inVar.Name, inVar.DoubleValue.Value); break;
                case ScriptVarType.Bool: SetInVar(inVar.Name, inVar.BoolValue.Value); break;
                case ScriptVarType.String: SetInVar(inVar.Name, inVar.StringValue); break;
                case ScriptVarType.Object: SetInVar(inVar.Name, inVar.ScriptObjectValue); break;
                case ScriptVarType.Null: SetInVar(inVar.Name, (IScriptObject)null); break;
                default: throw new ArgumentException("ScriptInVar type not supported.", nameof(inVar));
            }
        }

        public void Print(string value)
        {
            PrintHandler?.Invoke(this, new ScriptPrintEventArgs(value));
        }

        public void RunScript(Script script)
        {
            InitializeScript(script);
            RunScript();
        }

        public void RunScript(Script script, ScriptInVar inVar)
        {
            InitializeScript(script);
            SetInVar(inVar);
            RunScript();
        }

        public void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1)
        {
            InitializeScript(script);
            SetInVar(inVar0);
            SetInVar(inVar1);
            RunScript();
        }

        public void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1, ScriptInVar inVar2)
        {
            InitializeScript(script);
            SetInVar(inVar0);
            SetInVar(inVar1);
            SetInVar(inVar2);
            RunScript();
        }

        public void RunScript(Script script, ScriptInVar inVar0, ScriptInVar inVar1, ScriptInVar inVar2, ScriptInVar inVar3)
        {
            InitializeScript(script);
            SetInVar(inVar0);
            SetInVar(inVar1);
            SetInVar(inVar2);
            SetInVar(inVar3);
            RunScript();
        }

        public void RunScript(Script script, params ScriptInVar[] inVars)
        {
            RunScript(script, inVars.AsSpan());
        }

        public void RunScript(Script script, ReadOnlySpan<ScriptInVar> inVars)
        {
            InitializeScript(script);
            foreach (ScriptInVar inVar in inVars)
            {
                SetInVar(inVar);
            }
            RunScript();
        }

        public void RunScript(Script script, List<ScriptInVar> inVars)
        {
            InitializeScript(script);
            foreach (ScriptInVar inVar in inVars)
            {
                SetInVar(inVar);
            }
            RunScript();
        }

        private void RunScript()
        {
            if (_currentScript.CILInvoker != null)
            {
                switch (_currentScript.InVars)
                {
                    case 0:
                    {
                        ((Action<IScriptRuntime>)_currentScript.CILInvoker)(this);
                        break;
                    }
                    case 1:
                    {
                        var del = (Action<IScriptRuntime, ScriptVar>)_currentScript.CILInvoker;
                        del(this, _locals[0]);
                        break;
                    }
                    case 2:
                    {
                        var del = (Action<IScriptRuntime, ScriptVar, ScriptVar>)_currentScript.CILInvoker;
                        del(this, _locals[0], _locals[1]);
                        break;
                    }
                    case 3:
                    {
                        var del = (Action<IScriptRuntime, ScriptVar, ScriptVar, ScriptVar>)_currentScript.CILInvoker;
                        del(this, _locals[0], _locals[1], _locals[2]);
                        break;
                    }
                    case 4:
                    {
                        var del = (Action<IScriptRuntime, ScriptVar, ScriptVar, ScriptVar, ScriptVar>)_currentScript.CILInvoker;
                        del(this, _locals[0], _locals[1], _locals[2], _locals[3]);
                        break;
                    }
                    default:
                    {
                        var del = (Action<IScriptRuntime, ScriptVar[]>)_currentScript.CILInvoker;
                        del(this, _locals);
                        break;
                    }
                }
                return;
            }

            if (_exited)
            {
                return;
            }
            Script script = _currentScript;

            if (script.Locals + script.InVars > _locals.Length)
            {
                Error(ScriptErrorCode.R_MaxLocalsExceeded, "Max Locals Exceeded", $"Script requires {script.Locals + script.InVars} locals, but the runtime has only allocated {_locals.Length}");
            }
            else if (script.MaxStack > -1 && script.MaxStack >= StackSize)
            {
                Error(ScriptErrorCode.R_MaxStackSizeExceeded, "Max Stack Size Exceeded", $"Script requires {script.MaxStack} stack size, but the runtime has only allocated {StackCount}");
            }

            while (!_exited)
            {
                CallOp((ScriptOp)ReadByte());
            }

            if (StackCount > 0)
            {
                ErrorHandler?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorCode.R_StackNotEmpty, "Stack Not Empty", "Stack Not Empty. If no other errors were thrown, this is most likely a compiler issue.", ScriptErrorSeverity.Warning));
            }

            ClearLocals();
            _reference.WarnNotRemovedReferences(this);
            _reference.ClearReferences();
        }

        private void ClearLocals()
        {
            for (int i = 0; i < _usedLocals; i++)
            {
                if (_locals[i].IsRef)
                {
                    _reference.RemoveReference((int)_locals[i].GetObjectId());
                }
            }
        }

        public void Error(ScriptErrorCode code, string header, string message)
        {
            ErrorHandler?.Invoke(this, new ScriptErrorEventArgs(code, header, message, ScriptErrorSeverity.Error));
            _exited = true;
        }

        public void Warn(ScriptErrorCode code, string header, string message)
        {
            ErrorHandler?.Invoke(this, new ScriptErrorEventArgs(code, header, message, ScriptErrorSeverity.Warning));
        }

        public ScriptRuntime(int stackSize, int refSize, int locals)
        {
            InitStack(stackSize);
            _reference = new ScriptReference(refSize);
            _locals = new ScriptVar[locals];
            _invokeArgs = new List<ScriptVar>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                DisposeProgram();
                DisposeStack();
                DisposeOps();
                _disposedValue = true;
            }
        }

        ~ScriptRuntime()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
