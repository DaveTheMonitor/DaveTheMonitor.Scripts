using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Used to mark a class or interface as an ignored script type. This will prevent scripts from using it directly, but still allow it to specify methods with <see cref="ScriptMethodAttribute"/> that can be implemented in a type and called by scripts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ScriptTypeIgnoreAttribute : Attribute
    {
        public ScriptTypeIgnoreAttribute()
        {
            
        }
    }
}
