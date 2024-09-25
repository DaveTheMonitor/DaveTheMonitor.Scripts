using DaveTheMonitor.Scripts.Runtime;
using DaveTheMonitor.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaveTheMonitor.Scripts
{
    public sealed class Script
    {
        public string Name { get; private set; }
        public ScriptRuntimeType Type { get; private set; }
        public byte[] Bytecode { get; private set; }
        public string[] StringLiterals { get; private set; }
        public int Locals { get; private set; }
        public int InVars { get; private set; }
        public int MaxStack { get; private set; }
        public bool IsRunning { get; set; }
        public Delegate CILInvoker { get; internal set; }
        private Dictionary<string, ScriptInVarDefinition> _inVars;

        public static Script Create(string name, ScriptRuntimeType type)
        {
            return new Script(name, type);
        }

        public string GetStringLiteral(int index)
        {
            return StringLiterals[index];
        }

        public void SetStringLiterals(string[] literals)
        {
            StringLiterals = literals;
        }

        public string GetBytecodeString(bool showPos)
        {
            StringBuilder builder = new StringBuilder();
            ByteReader reader = new ByteReader(Bytecode);
            while (reader.Position < reader.Length)
            {
                ScriptOp op = (ScriptOp)reader.ReadByte();
                if (showPos)
                {
                    builder.Append($"{(reader.Position - 1).ToString().PadLeft(4, '0')} | ");
                }
                switch (op)
                {
                    case ScriptOp.DoubleLiteral: builder.AppendLine($"{op} {reader.ReadDouble()}"); break;
                    case ScriptOp.LongLiteral: builder.AppendLine($"{op} {reader.ReadInt64()}"); break;
                    case ScriptOp.StringLiteral: builder.AppendLine($"{op} \"{GetStringLiteral(reader.ReadUInt16())}\""); break;
                    case ScriptOp.SetLoc:
                    case ScriptOp.SetLoc_NoRef:
                    {
                        builder.AppendLine($"{op} {reader.ReadByte()}");
                        break;
                    }
                    case ScriptOp.LoadLoc:
                    case ScriptOp.LoadLoc_NoRef:
                    {
                        builder.AppendLine($"{op} {reader.ReadByte()}");
                        break;
                    }
                    case ScriptOp.Jump:
                    case ScriptOp.JumpT:
                    case ScriptOp.JumpF:
                    case ScriptOp.JumpEq:
                    case ScriptOp.JumpNeq:
                    case ScriptOp.JumpLt:
                    case ScriptOp.JumpLte:
                    case ScriptOp.JumpGt:
                    case ScriptOp.JumpGte:
                    case ScriptOp.JumpEq_Num:
                    case ScriptOp.JumpNeq_Num:
                    case ScriptOp.JumpLt_Num:
                    case ScriptOp.JumpLte_Num:
                    case ScriptOp.JumpGt_Num:
                    case ScriptOp.JumpGte_Num:
                    {
                        builder.AppendLine($"{op} {reader.ReadInt32()}");
                        break;
                    }
                    case ScriptOp.Invoke:
                    case ScriptOp.InvokeStatic:
                    {
                        ScriptMethod method = ScriptMethod.GetMethod(reader.ReadInt32());
                        builder.AppendLine($"{op} {(method.DeclaringType?.Name ?? "static")}:{(method.IsCtor ? "ctor" : method.Name)}");
                        break;
                    }
                    case ScriptOp.InvokeDynamic: builder.AppendLine($"{op} \"{GetStringLiteral(reader.ReadUInt16())}\" {reader.ReadUInt16()}"); break;
                    case ScriptOp.SetDynamicProperty: builder.AppendLine($"{op} \"{GetStringLiteral(reader.ReadUInt16())}\""); break;
                    case ScriptOp.GetProperty:
                    case ScriptOp.SetProperty:
                    case ScriptOp.GetStaticProperty:
                    case ScriptOp.SetStaticProperty:
                    {
                        builder.AppendLine($"{op} {ScriptProperty.GetProperty(reader.ReadInt32()).Name}");
                        break;
                    }
                    case ScriptOp.CheckType:
                    case ScriptOp.PeekCheckType:
                    {
                        builder.AppendLine($"{op} {ScriptType.GetType(reader.ReadInt32()).Name}");
                        break;
                    }
                    case ScriptOp.Loop: builder.AppendLine($"{op} {reader.ReadInt32()} {reader.ReadInt32()}"); break;
                    case ScriptOp.Loop_Num: builder.AppendLine($"{op} {reader.ReadInt32()}"); break;
                    default: builder.AppendLine(op.ToString()); break;
                }
            }
            return builder.ToString();
        }

        public bool SetBytecode(byte[] bytecode, int locals, int maxStack, Dictionary<string, ScriptInVarDefinition> inVars)
        {
            if (IsRunning)
            {
                return false;
            }
            Locals = locals;
            InVars = inVars.Count;
            MaxStack = maxStack;
            Bytecode = bytecode;
            _inVars = inVars;
            return true;
        }

        public ScriptInVarDefinition GetInVar(string name)
        {
            _inVars.TryGetValue(name, out ScriptInVarDefinition definition);
            return definition;
        }

        public bool TryGetInVar(string name, out ScriptInVarDefinition definition)
        {
            return _inVars.TryGetValue(name, out definition);
        }

        public ScriptInVarDefinition[] GetAllInVars()
        {
            return _inVars.Values.ToArray();
        }

        private Script(string name, ScriptRuntimeType type)
        {
            Name = name;
            Type = type;
            Bytecode = Array.Empty<byte>();
            IsRunning = false;
        }
    }
}
