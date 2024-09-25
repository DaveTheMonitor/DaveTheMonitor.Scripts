using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a script constant. Script constants are inlined by the compiler and cannot change. This attribute is only valid on constant members. To ensure continued script functionality, do not change constant values between mod releases.
    /// If the value can change or is not a true constant (eg. save version that changes with each update should not be a const), use a ScriptProperty without a setter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScriptConstAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public ScriptConstAttribute()
        {
            
        }
    }
}
