using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a ScriptType as iterable, allowing it to be used with a Foreach loop. Count and GetItem must be the names of methods with the ScriptMethod or ScriptProperty attribute.
    /// </summary>
    /// <remarks>
    /// <para>Count must be a ScriptMethod or ScriptProperty that returns an int.</para>
    /// <para>GetItem must be a ScriptMethod.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ScriptIteratorAttribute : Attribute
    {
        public string Count { get; set; }
        public string GetItem { get; set; }

        public ScriptIteratorAttribute()
        {

        }
    }
}
