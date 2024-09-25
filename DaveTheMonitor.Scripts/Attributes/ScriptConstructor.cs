using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a static method as callable by scripts as a type constructor. This allows scripts to create instances of this type using [new Object]
    /// </summary>
    /// <remarks>
    /// <para>The declaring type must be a valid ScriptType.</para>
    /// <para>Each type can only contain one ScriptConstructor.</para>
    /// <para>The method must be static and return the declaring type.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ScriptConstructorAttribute : Attribute
    {
        public ScriptConstructorAttribute()
        {
            
        }
    }
}
