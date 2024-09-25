using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Used to change the name of a ScriptType. By default, ScriptType names are the same as the type name. This attribute is also used to declare a type that doesn't implement <see cref="IScriptObject"/> as containing static script members.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ScriptTypeAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public ScriptTypeAttribute()
        {
            
        }
    }
}
