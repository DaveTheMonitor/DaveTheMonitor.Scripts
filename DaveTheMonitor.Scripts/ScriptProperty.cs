using DaveTheMonitor.Scripts.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptProperty : ScriptMember
    {
        public static int PropertyCount => _properties.Length;
        public ScriptType PropertyType { get; private set; }
        public MemberInfo Member { get; private set; }
        public bool CanSet => _setDel != null;
        public bool CanGet => _getDel != null;
        private static ScriptProperty[] _properties;
        private DynamicMethod _getMethod;
        private DynamicMethod _setMethod;
        private Action<IScriptObject, IScriptRuntime, ScriptVar> _setDel;
        private Func<IScriptObject, IScriptRuntime, ScriptVar> _getDel;

        static ScriptProperty()
        {
            _properties = Array.Empty<ScriptProperty>();
        }

        public static ScriptProperty GetProperty(int id)
        {
            if (_properties.Length <= id)
            {
                return null;
            }
            return _properties[id];
        }

        public static ScriptProperty GetProperty(ScriptType type, string name)
        {
            return type.GetProperty(name);
        }

        public static ScriptProperty GetStaticProperty(string @namespace, string name)
        {
            foreach (ScriptProperty property in _properties)
            {
                if (property.IsStatic && property.Name == name && property.Namespace == @namespace)
                {
                    return property;
                }
            }
            return null;
        }

        public static List<ScriptProperty> GetStaticProperties(List<string> namespaces, string name)
        {
            List<ScriptProperty> l = new List<ScriptProperty>();
            foreach (ScriptProperty property in _properties)
            {
                if (property.IsStatic && property.Name == name)
                {
                    if (namespaces.Contains(property.Namespace))
                    {
                        l.Add(property);
                    };
                }
            }
            return l;
        }

        public static IReadOnlyList<ScriptProperty> GetAllProperies()
        {
            return _properties;
        }

        public void Set(IScriptObject obj, IScriptRuntime runtime, ScriptVar value)
        {
            _setDel(obj, runtime, value);
        }

        public ScriptVar Get(IScriptObject obj, IScriptRuntime runtime)
        {
            return _getDel(obj, runtime);
        }

        internal static ScriptProperty Create(ScriptType declaringType, PropertyInfo property, ScriptType propertyType, ScriptPropertyAccess access, string name)
        {
            ScriptProperty scriptProperty = new ScriptProperty(declaringType, property, propertyType, access, name);
            Add(scriptProperty);
            return scriptProperty;
        }

        internal static ScriptProperty Create(ScriptType declaringType, FieldInfo field, ScriptType propertyType, ScriptPropertyAccess access)
        {
            ScriptProperty property = new ScriptProperty(declaringType, field, propertyType, access);
            Add(property);
            return property;
        }

        private static void Add(ScriptProperty property)
        {
            property.Id = _properties.Length;
            Array.Resize(ref _properties, _properties.Length + 1);
            _properties[property.Id] = property;
        }

        private void ConvertFromScriptVar(ILGenerator il, Type type)
        {
            if (type == typeof(ScriptVar))
            {
                return;
            }
            else if (type == typeof(double))
            {
                il.EmitCall(OpCodes.Call, typeof(ScriptVar).GetMethod(nameof(ScriptVar.GetDoubleValue)), null);
            }
            else if (type == typeof(long))
            {
                il.EmitCall(OpCodes.Call, typeof(ScriptVar).GetMethod(nameof(ScriptVar.GetLongValue)), null);
            }
            else if (type == typeof(string))
            {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, typeof(ScriptVar).GetMethod(nameof(ScriptVar.GetStringValue)), null);
            }
            else if (type == typeof(IScriptObject))
            {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, typeof(ScriptVar).GetMethod(nameof(ScriptVar.GetObjectValue)), null);
            }
            else
            {
                // type here should be an implementation of IScriptObject
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, typeof(IScriptRuntime).GetProperty(nameof(IScriptRuntime.Reference)).GetMethod, null);
                il.EmitCall(OpCodes.Call, typeof(ScriptVar).GetMethod(nameof(ScriptVar.GetObjectValue)), null);
                il.Emit(OpCodes.Castclass, PropertyType.Type);
            }
        }

        public override string ToString()
        {
            return IsStatic ? $"Static:{Name}" : $"{DeclaringType.Name}:{Name}";
        }

        private ScriptProperty()
        {

        }

        internal ScriptProperty(ScriptType delcaringType, PropertyInfo property, ScriptType propertyType, ScriptPropertyAccess access, string name)
        {
            Type scriptVar = typeof(ScriptVar);
            IsStatic = (property.GetMethod?.IsStatic ?? property.SetMethod.IsStatic);
            DeclaringType = IsStatic ? null : delcaringType;
            Member = property;
            ScriptPropertyAttribute attribute = property.GetCustomAttribute<ScriptPropertyAttribute>();
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
                ScriptTypeAttribute typeAttribute = property.DeclaringType.GetCustomAttribute<ScriptTypeAttribute>();
                if (typeAttribute?.Namespace != null)
                {
                    Namespace = typeAttribute.Namespace.ToLowerInvariant();
                }
                else
                {
                    Namespace = ScriptType.GetDefaultNamespace(property.DeclaringType);
                }
            }
            Name = name;
            PropertyType = propertyType ?? ScriptType.GetType(typeof(ScriptVar));
            Type ret = propertyType?.Type ?? scriptVar;

            if (access == ScriptPropertyAccess.Default)
            {
                if (property.CanRead && property.GetMethod.IsPublic)
                {
                    access ^= ScriptPropertyAccess.Get;
                }
                if (property.CanWrite && property.SetMethod.IsPublic)
                {
                    access ^= ScriptPropertyAccess.Set;
                }
            }

            if ((access & ScriptPropertyAccess.Get) > 0)
            {
                _getMethod = new DynamicMethod($"ScriptPropertyGet", scriptVar, new Type[] { typeof(IScriptObject), typeof(IScriptRuntime) });
                ILGenerator il = _getMethod.GetILGenerator();

                if (!IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, DeclaringType.Type);
                }
                il.EmitCall(IsStatic ? OpCodes.Call : OpCodes.Callvirt, property.GetMethod, null);
                if (ret != scriptVar)
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

                _getDel = _getMethod.CreateDelegate<Func<IScriptObject, IScriptRuntime, ScriptVar>>();
            }
            if ((access & ScriptPropertyAccess.Set) > 0)
            {
                _setMethod = new DynamicMethod($"ScriptPropertySet", null, new Type[] { typeof(IScriptObject), typeof(IScriptRuntime), typeof(ScriptVar) });
                ILGenerator il = _setMethod.GetILGenerator();

                if (!IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, DeclaringType.Type);
                }
                il.Emit(OpCodes.Ldarga_S, 2);

                ConvertFromScriptVar(il, PropertyType.Type);

                il.EmitCall(IsStatic ? OpCodes.Call : OpCodes.Callvirt, property.SetMethod, null);
                il.Emit(OpCodes.Ret);

                _setDel = _setMethod.CreateDelegate<Action<IScriptObject, IScriptRuntime, ScriptVar>>();
            }
        }

        internal ScriptProperty(ScriptType delcaringType, FieldInfo field, ScriptType propertyType, ScriptPropertyAccess access)
        {
            Type scriptVar = typeof(ScriptVar);
            IsStatic = field.IsStatic;
            DeclaringType = IsStatic ? null : delcaringType;
            Member = field;
            ScriptPropertyAttribute attribute = field.GetCustomAttribute<ScriptPropertyAttribute>();
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
                ScriptTypeAttribute typeAttribute = field.DeclaringType.GetCustomAttribute<ScriptTypeAttribute>();
                if (typeAttribute?.Namespace != null)
                {
                    Namespace = typeAttribute.Namespace.ToLowerInvariant();
                }
                else
                {
                    Namespace = ScriptType.GetDefaultNamespace(field.DeclaringType);
                }
            }
            Name = (attribute.Name ?? field.Name).ToLowerInvariant();
            PropertyType = propertyType ?? ScriptType.GetType(typeof(ScriptVar));
            Type ret = propertyType?.Type ?? scriptVar;

            if (access == ScriptPropertyAccess.Default)
            {
                access = ScriptPropertyAccess.Get | ScriptPropertyAccess.Set;
            }

            if ((access & ScriptPropertyAccess.Get) > 0)
            {
                _getMethod = new DynamicMethod($"ScriptPropertyGet", scriptVar, new Type[] { typeof(IScriptObject), typeof(IScriptRuntime) });
                ILGenerator il = _getMethod.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, DeclaringType.Type);
                il.Emit(OpCodes.Ldfld, field);
                if (ret != scriptVar)
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

                _getDel = _getMethod.CreateDelegate<Func<IScriptObject, IScriptRuntime, ScriptVar>>();
            }
            if ((access & ScriptPropertyAccess.Set) > 0)
            {
                _setMethod = new DynamicMethod($"ScriptPropertySet", null, new Type[] { typeof(IScriptObject), typeof(IScriptRuntime), typeof(ScriptVar) });
                ILGenerator il = _setMethod.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, DeclaringType.Type);
                il.Emit(OpCodes.Ldarga_S, 2);

                ConvertFromScriptVar(il, PropertyType.Type);

                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);

                _setDel = _setMethod.CreateDelegate<Action<IScriptObject, IScriptRuntime, ScriptVar>>();
            }
        }
    }
}
