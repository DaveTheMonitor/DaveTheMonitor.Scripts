using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Declares a method as callable by scripts. The method will be callable using the same name as the method or whatever is specified for this attribute. If the method is static, it will not be associated with the declaring type. If the method is not static, either the declaring type or one of its base classes must implement IScriptObject.
    /// </summary>
    /// <remarks>
    /// When storing a ScriptVar argument, the method or declaring type is responsible for adding and removing its reference if it is an object.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ScriptMethodAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public ScriptMethodAttribute()
        {
            
        }
    }
}
