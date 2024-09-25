using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a field as the static ScriptType field for IScriptObject implementations. If no field has this attribute, the ScriptType field will be used (if it exists)
    /// </summary>
    /// <remarks>
    /// It is highly recommended for IScriptObject implementations to implement IScriptObject.Type using this field for performance.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScriptTypeFieldAttribute : Attribute
    {
        public ScriptTypeFieldAttribute()
        {

        }
    }
}
