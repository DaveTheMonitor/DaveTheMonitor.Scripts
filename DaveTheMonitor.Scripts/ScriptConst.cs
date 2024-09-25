using DaveTheMonitor.Scripts.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptConst : ScriptMember
    {
        public ScriptType Type { get; private set; }
        public FieldInfo Field { get; private set; }
        public object Value { get; private set; }
        private static ScriptConst[] _consts;
        private static int _ids;

        static ScriptConst()
        {
            _consts = Array.Empty<ScriptConst>();
            _ids = 0;
        }

        public static ScriptConst GetConst(int id)
        {
            return _consts[id];
        }

        public static ScriptConst GetConst(string @namespace, string name)
        {
            foreach (ScriptConst constant in _consts)
            {
                if (constant.Name == name && constant.Namespace == @namespace)
                {
                    return constant;
                }
            }
            return null;
        }

        public static List<ScriptConst> GetConsts(List<string> namespaces, string name)
        {
            List<ScriptConst> l = new List<ScriptConst>();
            foreach (ScriptConst constant in _consts)
            {
                if (constant.IsStatic && constant.Name == name)
                {
                    if (namespaces.Contains(constant.Namespace))
                    {
                        l.Add(constant);
                    };
                }
            }
            return l;
        }

        public static IReadOnlyList<ScriptConst> GetAllConsts()
        {
            return _consts;
        }

        internal static ScriptConst Create(FieldInfo field, ScriptType type, object value)
        {
            ScriptConst constant = new ScriptConst(field, type, value);
            constant.Id = _ids++;
            Array.Resize(ref _consts, _consts.Length + 1);
            _consts[constant.Id] = constant;
            return constant;
        }

        public override string ToString()
        {
            return $"{Name}:{Value}";
        }

        private ScriptConst()
        {

        }

        private ScriptConst(FieldInfo field, ScriptType type, object value)
        {
            DeclaringType = null;
            Type = type;
            Field = field;
            ScriptConstAttribute attribute = field.GetCustomAttribute<ScriptConstAttribute>();
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
            Value = value;
            IsStatic = true;
        }
    }
}
