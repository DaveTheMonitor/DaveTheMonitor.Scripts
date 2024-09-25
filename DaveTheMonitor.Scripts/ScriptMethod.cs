using DaveTheMonitor.Scripts.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DaveTheMonitor.Scripts
{
    public abstract class ScriptMethod : ScriptMember
    {
        public ScriptType[] Args { get; protected set; }
        public ScriptType ReturnType { get; protected set; }
        public MethodInfo Method { get; protected set; }
        public bool RuntimeArg { get; protected set; }
        protected DynamicMethod DynamicMethod;
        private static ScriptMethod[] _methods;

        static ScriptMethod()
        {
            _methods = Array.Empty<ScriptMethod>();
        }

        public static ScriptMethod GetMethod(int id)
        {
            if (_methods.Length <= id)
            {
                return null;
            }
            return _methods[id];
        }

        public static ScriptMethod GetMethod(ScriptType type, string name)
        {
            return type.GetMethod(name);
        }

        public static ScriptMethod GetStaticMethod(string @namespace, string name)
        {
            foreach (ScriptMethod method in _methods)
            {
                if (method.IsStatic && method.Name == name && method.Namespace == @namespace)
                {
                    return method;
                }
            }
            return null;
        }

        public static List<ScriptMethod> GetStaticMethods(List<string> namespaces, string name)
        {
            List<ScriptMethod> l = new List<ScriptMethod>();
            foreach (ScriptMethod method in _methods)
            {
                if (method.IsStatic && method.Name == name)
                {
                    if (namespaces.Contains(method.Namespace))
                    {
                        l.Add(method);
                    }
                }
            }
            return l;
        }

        public abstract ScriptVar Invoke(IScriptObject obj, IScriptRuntime runtime, List<ScriptVar> args);

        private static void EmitArg(ILGenerator il, int i, ScriptType type)
        {
            Type scriptVar = typeof(ScriptVar);
            il.Emit(OpCodes.Ldarg_2);

            switch (i)
            {
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                {
                    if (i < 255)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (byte)i);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, i);
                    }
                    break;
                }
            }

            il.EmitCall(OpCodes.Call, typeof(List<ScriptVar>).GetMethod("get_Item"), null);
            if (type.Type == scriptVar)
            {
                return;
            }

            if (type.Type == typeof(double))
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetDoubleValue)), null);
            }
            else if (type.Type == typeof(long))
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetLongValue)), null);
            }
            else if (type.Type == typeof(bool))
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetBoolValue)), null);
            }
            else if (type.Type == typeof(string))
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetStringValue)), null);
            }
            else if (type.Type == typeof(IScriptObject))
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue)), null);
            }
            else
            {
                // type here should be an implementation of IScriptObject
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, scriptVar.GetMethod(nameof(ScriptVar.GetObjectValue)), null);
                il.Emit(OpCodes.Castclass, type.Type);
            }
        }

        internal static ScriptMethod Create(ScriptType declaringType, MethodInfo method, ScriptType returnType, ScriptType[] args, string name)
        {
            ScriptMethod scriptMethod;
            if (args?.Length > 0)
            {
                scriptMethod = new ScriptMethodArgs(declaringType, method, returnType, args, name);
            }
            else
            {
                scriptMethod = new ScriptMethodRet(declaringType, method, returnType, name);
            }

            scriptMethod.Id = _methods.Length;
            Array.Resize(ref _methods, _methods.Length + 1);
            _methods[scriptMethod.Id] = scriptMethod;
            return scriptMethod;
        }

        public override string ToString()
        {
            return IsStatic ? $"Static:{Name}" : $"{DeclaringType.Name}:{Name}";
        }

        private ScriptMethod()
        {

        }

        private void InitMethod(MethodInfo method)
        {
            ScriptMethodAttribute attribute = method.GetCustomAttribute<ScriptMethodAttribute>();
            if (attribute?.Namespace != null)
            {
                Namespace = attribute.Namespace.ToLowerInvariant();
            }
            else if (DeclaringType != null)
            {
                Namespace = DeclaringType.Namespace;
            }
            else
            {
                ScriptTypeAttribute typeAttribute = method.DeclaringType.GetCustomAttribute<ScriptTypeAttribute>();
                if (typeAttribute?.Namespace != null)
                {
                    Namespace = typeAttribute.Namespace.ToLowerInvariant();
                }
                else
                {
                    Namespace = ScriptType.GetDefaultNamespace(method.DeclaringType);
                }
            }
        }

        private void InitCtor(MethodInfo method)
        {
            Namespace = DeclaringType.Namespace;
        }

        protected ScriptMethod(ScriptType declaringType, MethodInfo method, ScriptType returnType, ScriptType[] args, string name)
        {
            Type scriptVar = typeof(ScriptVar);
            IsCtor = method.GetCustomAttribute<ScriptConstructorAttribute>() != null;
            IsStatic = method.IsStatic;
            DeclaringType = (IsStatic && !IsCtor) ? null : declaringType;
            Method = method;
            Name = name;
            if (IsCtor)
            {
                InitCtor(method);
            }
            else
            {
                InitMethod(method);
            }
            ParameterInfo[] @params = method.GetParameters();
            RuntimeArg = @params.Length > 0 && @params[0].ParameterType == typeof(IScriptRuntime);
            Args = args ?? Array.Empty<ScriptType>();
            ReturnType = returnType ?? ScriptType.GetType(typeof(void));

            Type ret = returnType?.Type ?? scriptVar;
            Type[] argsArray;
            if (args != null && args.Length > 0)
            {
                argsArray = new Type[]
                {
                    typeof(IScriptObject),
                    typeof(IScriptRuntime),
                    typeof(List<ScriptVar>)
                };
            }
            else
            {
                argsArray = new Type[]
                {
                    typeof(IScriptObject),
                    typeof(IScriptRuntime)
                };
            }

            DynamicMethod = new DynamicMethod($"ScriptInvoke", scriptVar, argsArray);
            ILGenerator il = DynamicMethod.GetILGenerator();
            if (!IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, DeclaringType.Type);
            }
            if (RuntimeArg)
            {
                il.Emit(OpCodes.Ldarg_1);
            }

            if (args != null && args.Length > 0)
            {
                foreach (ScriptType t in args)
                {
                    if (t.Type != scriptVar)
                    {
                        // because we need to call ScriptVar.Get* for known types
                        // we need a local to get the address with ldloca
                        // This local is not needed if every arg is ScriptVar
                        il.DeclareLocal(scriptVar);
                        break;
                    }
                }

                for (int i = 0; i < args.Length; i++)
                {
                    EmitArg(il, i, args[i]);
                }
            }

            il.EmitCall(IsStatic ? OpCodes.Call : OpCodes.Callvirt, method, null);
            if (method.ReturnType == typeof(void))
            {
                il.EmitCall(OpCodes.Call, scriptVar.GetProperty("Null").GetMethod, null);
            }
            else if (ret != scriptVar)
            {
                if (ret == typeof(double) || ret == typeof(bool))
                {
                    il.Emit(OpCodes.Newobj, scriptVar.GetConstructor(new Type[] { ret }));
                }
                else if (ret == typeof(long))
                {
                    il.Emit(OpCodes.Conv_R8);
                    il.Emit(OpCodes.Newobj, scriptVar.GetConstructor(new Type[] { typeof(double) }));
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                    il.Emit(OpCodes.Newobj, scriptVar.GetConstructor(new Type[] { ret == typeof(string) ? ret : typeof(IScriptObject), typeof(IScriptReference) }));
                }
            }
            il.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ScriptMethodRet : ScriptMethod
    {
        private Func<IScriptObject, IScriptRuntime, ScriptVar> _del;

        public override ScriptVar Invoke(IScriptObject obj, IScriptRuntime runtime, List<ScriptVar> args)
        {
            return _del(obj, runtime);
        }

        public override string ToString()
        {
            return IsStatic ? $"Static:{Name} [out:{ReturnType.Name}]" : $"{DeclaringType.Name}:{Name} [out:{ReturnType.Name}]";
        }

        internal ScriptMethodRet(ScriptType declaringType, MethodInfo method, ScriptType returnType, string name) : base(declaringType, method, returnType, null, name)
        {
            _del = DynamicMethod.CreateDelegate<Func<IScriptObject, IScriptRuntime, ScriptVar>>();
        }
    }

    internal sealed class ScriptMethodArgs : ScriptMethod
    {
        private Func<IScriptObject, IScriptRuntime, List<ScriptVar>, ScriptVar> _del;

        public override ScriptVar Invoke(IScriptObject obj, IScriptRuntime runtime, List<ScriptVar> args)
        {
            return _del(obj, runtime, args);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(IsStatic ? $"Static:{Name} [out:{ReturnType.Name}]" : $"{DeclaringType.Name}:{Name} [out:{ReturnType.Name}]");
            foreach (ScriptType t in Args)
            {
                builder.Append($" [{t.Name}]");
            }
            return builder.ToString();
        }

        internal ScriptMethodArgs(ScriptType declaringType, MethodInfo method, ScriptType returnType, ScriptType[] args, string name) : base(declaringType, method, returnType, args, name)
        {
            _del = DynamicMethod.CreateDelegate<Func<IScriptObject, IScriptRuntime, List<ScriptVar>, ScriptVar>>();
        }
    }
}
