using System;

namespace DaveTheMonitor.Scripts.Attributes
{
    /// <summary>
    /// Used to change the default script namespace of all types in an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ScriptAssemblyNamespaceAttribute : Attribute
    {
        public string Namespace { get; set; }

        public ScriptAssemblyNamespaceAttribute(string @namespace)
        {
            Namespace = @namespace;
        }
    }
}
