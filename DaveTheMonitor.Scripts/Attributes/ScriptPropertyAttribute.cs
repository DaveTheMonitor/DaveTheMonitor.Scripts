using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a property as accessible by scripts. By default, only public setters/getters can be used by scripts unless otherwise specified. If there is no getter or setter it cannot be used. If used on a field, scripts can get/set them by default.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScriptPropertyAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        // This doesn't work in TM if it's a property?
        // Throws "CustomAttributeFormatException: 'Access' property specified was not found"
        // when any usage specifies Access, but not when they don't.
        // This exception is only thrown in TM, not when testing or running the sandbox/console
        // wtf?
        public ScriptPropertyAccess Access = ScriptPropertyAccess.Default;

        public ScriptPropertyAttribute()
        {
            
        }
    }
}
