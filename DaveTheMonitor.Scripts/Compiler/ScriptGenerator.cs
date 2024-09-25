using DaveTheMonitor.Scripts.Runtime;
using DaveTheMonitor.Scripts.Utilities;
using System;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Compiler
{
    public sealed class ScriptGenerator
    {
        public string ScriptName { get; set; }
        public int Position { get => _writer.Position; set => _writer.Position = value; }
        private ByteWriter _writer;
        private List<string> _stringLiterals;
        int _locals;

        public void Write(ScriptOp op)
        {
            Write(op, null, null);
        }

        public void Write(ScriptOp op, object arg)
        {
            Write(op, arg, null);
        }

        public void Write(ScriptOp op, object arg1, object arg2)
        {
            _writer.Write((byte)op);
            Write(arg1);
            Write(arg2);
        }

        public void WriteInvoke(ScriptMethod method)
        {
            _writer.Write(method.IsStatic ? (byte)ScriptOp.InvokeStatic : (byte)ScriptOp.Invoke);
            _writer.Write(method.Id);
        }

        public void WriteGetProperty(ScriptProperty property)
        {
            _writer.Write(property.IsStatic ? (byte)ScriptOp.GetStaticProperty : (byte)ScriptOp.GetProperty);
            _writer.Write(property.Id);
        }

        public void WriteSetProperty(ScriptProperty property)
        {
            _writer.Write(property.IsStatic ? (byte)ScriptOp.SetStaticProperty : (byte)ScriptOp.SetProperty);
            _writer.Write(property.Id);
        }

        public void WriteInvokeDynamic(string name, int args)
        {
            _writer.Write((byte)ScriptOp.InvokeDynamic);
            _writer.Write((ushort)GetStringLiteral(name));
            _writer.Write((ushort)args);
        }

        public void WriteSetDynamicProperty(string name)
        {
            _writer.Write((byte)ScriptOp.SetDynamicProperty);
            _writer.Write((ushort)GetStringLiteral(name));
        }

        public void WriteSetLoc(byte local)
        {
            WriteSetLoc(local, false);
        }

        public void WriteSetLocNoRef(byte local)
        {
            WriteSetLoc(local, true);
        }

        private void WriteSetLoc(byte local, bool noRef)
        {
            _locals = Math.Max(local + 1, _locals);
            switch (local)
            {
                case 0: _writer.Write(noRef ? (byte)ScriptOp.SetLoc_0_NoRef : (byte)ScriptOp.SetLoc_0); break;
                case 1: _writer.Write(noRef ? (byte)ScriptOp.SetLoc_1_NoRef : (byte)ScriptOp.SetLoc_1); break;
                case 2: _writer.Write(noRef ? (byte)ScriptOp.SetLoc_2_NoRef : (byte)ScriptOp.SetLoc_2); break;
                case 3: _writer.Write(noRef ? (byte)ScriptOp.SetLoc_3_NoRef : (byte)ScriptOp.SetLoc_3); break;
                default:
                {
                    _writer.Write(noRef ? (byte)ScriptOp.SetLoc_NoRef : (byte)ScriptOp.SetLoc);
                    _writer.Write(local);
                    break;
                }
            }
        }

        public void WriteLoadLoc(byte local)
        {
            WriteLoadLoc(local, false);
        }

        public void WriteLoadLocNoRef(byte local)
        {
            WriteLoadLoc(local, true);
        }

        private void WriteLoadLoc(byte local, bool noRef)
        {
            _locals = Math.Max(local + 1, _locals);
            switch (local)
            {
                case 0: _writer.Write(noRef ? (byte)ScriptOp.LoadLoc_0_NoRef : (byte)ScriptOp.LoadLoc_0); break;
                case 1: _writer.Write(noRef ? (byte)ScriptOp.LoadLoc_1_NoRef : (byte)ScriptOp.LoadLoc_1); break;
                case 2: _writer.Write(noRef ? (byte)ScriptOp.LoadLoc_2_NoRef : (byte)ScriptOp.LoadLoc_2); break;
                case 3: _writer.Write(noRef ? (byte)ScriptOp.LoadLoc_3_NoRef : (byte)ScriptOp.LoadLoc_3); break;
                default:
                {
                    _writer.Write(noRef ? (byte)ScriptOp.LoadLoc_NoRef : (byte)ScriptOp.LoadLoc);
                    _writer.Write(local);
                    break;
                }
            }
        }

        public void WriteNullLiteral()
        {
            _writer.Write((byte)ScriptOp.NullLiteral);
        }

        public void WriteLiteral(double value)
        {
            switch (value)
            {
                case 0f: _writer.Write((byte)ScriptOp.DoubleLiteral_0); break;
                case 1f: _writer.Write((byte)ScriptOp.DoubleLiteral_1); break;
                default:
                {
                    _writer.Write((byte)ScriptOp.DoubleLiteral);
                    _writer.Write(value);
                    break;
                }
            }
        }

        public void WriteLiteral(long value)
        {
            switch (value)
            {
                case 0: _writer.Write((byte)ScriptOp.LongLiteral_0); break;
                case 1: _writer.Write((byte)ScriptOp.LongLiteral_1); break;
                default:
                {
                    _writer.Write((byte)ScriptOp.LongLiteral);
                    _writer.Write(value);
                    break;
                }
            }
        }

        public void WriteLiteral(bool value)
        {
            _writer.Write(value ? (byte)ScriptOp.TrueLiteral : (byte)ScriptOp.FalseLiteral);
        }

        public void WriteLiteral(string value)
        {
            _writer.Write((byte)ScriptOp.StringLiteral);
            _writer.Write((ushort)GetStringLiteral(value));
        }

        private int GetStringLiteral(string value)
        {
            int index = _stringLiterals.IndexOf(value);
            if (index == -1)
            {
                index = _stringLiterals.Count;
                _stringLiterals.Add(value);
            }
            return index;
        }

        public void Write(object value)
        {
            if (value == null)
            {
                return;
            }

            if (value is byte b)
            {
                _writer.Write(b);
            }
            else if (value is sbyte sb)
            {
                _writer.Write(sb);
            }
            else if (value is short s)
            {
                _writer.Write(s);
            }
            else if (value is ushort us)
            {
                _writer.Write(us);
            }
            else if (value is int i)
            {
                _writer.Write(i);
            }
            else if (value is uint ui)
            {
                _writer.Write(ui);
            }
            else if (value is long l)
            {
                _writer.Write(l);
            }
            else if (value is ulong ul)
            {
                _writer.Write(ul);
            }
            else if (value is Half h)
            {
                _writer.Write(h);
            }
            else if (value is float f)
            {
                _writer.Write(f);
            }
            else if (value is double d)
            {
                _writer.Write(d);
            }
            else if (value is decimal m)
            {
                _writer.Write(m);
            }
            else if (value is string str)
            {
                _writer.Write(str);
            }
        }

        public Script CreateScript(ScriptRuntimeType type, int maxStack, Dictionary<string, ScriptInVarDefinition> inVars)
        {
            byte[] bytes = _writer.GetBytes();
            Script script = Script.Create(ScriptName, type);
            script.SetBytecode(bytes, _locals, maxStack, inVars);
            script.SetStringLiterals(_stringLiterals.ToArray());
            return script;
        }

        public ScriptGenerator(string scriptName)
        {
            ScriptName = scriptName;
            _writer = new ByteWriter(4);
            _stringLiterals = new List<string>();
        }

        public ScriptGenerator(Script script)
        {
            ScriptName = script.Name;
            _writer = new ByteWriter(script.Bytecode);
            _stringLiterals = new List<string>();
        }
    }
}
