using System;

namespace DaveTheMonitor.Scripts
{
    public readonly struct ScriptInVar
    {
        public ScriptVarType Type { get; private init; }
        public string Name { get; private init; }
        public double? DoubleValue => Type == ScriptVarType.Double ? _doubleValue : null;
        public bool? BoolValue => Type == ScriptVarType.Bool ? DoubleValue > 0 : null;
        public IScriptObject ScriptObjectValue => _objectValue as IScriptObject;
        public string StringValue => _objectValue as string;
        private readonly double _doubleValue;
        private readonly object _objectValue;

        public ScriptInVar(string name, object value)
        {
            Name = name;
            if (value is double d)
            {
                Type = ScriptVarType.Double;
                _doubleValue = d;
            }
            else if (value is bool b)
            {
                Type = ScriptVarType.Bool;
                _doubleValue = b ? 1 : 0;
            }
            else if (value is string s)
            {
                Type = ScriptVarType.String;
                _objectValue = value;
            }
            else if (value is IScriptObject)
            {
                Type = ScriptVarType.Object;
                _objectValue = value;
            }
            else
            {
                throw new ArgumentException("ScriptInVar type must be double, bool, string, or IScriptObject.", nameof(value));
            }
        }

        public ScriptInVar(string name, double value)
        {
            Type = ScriptVarType.Double;
            Name = name;
            _doubleValue = value;
        }

        public ScriptInVar(string name, bool value)
        {
            Type = ScriptVarType.Bool;
            Name = name;
            _doubleValue = value ? 1 : 0;
        }

        public ScriptInVar(string name, string value)
        {
            Name = name;
            if (value == null)
            {
                Type = ScriptVarType.Null;
            }
            else
            {
                Type = ScriptVarType.String;
                _objectValue = value;
            }
        }

        public ScriptInVar(string name, IScriptObject value)
        {
            Name = name;
            if (value == null)
            {
                Type = ScriptVarType.Null;
            }
            else
            {
                Type = ScriptVarType.Object;
                _objectValue = value;
            }
        }
    }
}
