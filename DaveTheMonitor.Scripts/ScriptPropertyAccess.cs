using System;

namespace DaveTheMonitor.Scripts
{
    [Flags]
    public enum ScriptPropertyAccess : byte
    {
        Default = 0b00,
        Get = 0b01,
        Set = 0b10
    }
}
