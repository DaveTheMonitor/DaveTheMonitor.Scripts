using DaveTheMonitor.Scripts;
using DaveTheMonitor.Scripts.Attributes;
using System;

namespace DaveTheMonitor.ScriptConsole
{
    // This contains an example object that can be used by scripts.

    // This attribute is only required in the following situations:
    // - if the name you want scripts to reference is different than the type name
    // - if the type is static, this attribute specifies that the type contains static functions
    // - if the namespace you want this type to belong to is different than the namespace in AssemblyInfo
    //
    // Script types can also implement enumerators for scripts. See ScriptArrayVar for more info. Adding
    // the ScriptIterator attribute allows scripts to use Foreach on the object.
    [ScriptType(Name = "exampleobject")]
    public sealed class ExampleObject : IScriptObject
    {
        // A property that can be accessed by scripts. This attribute can be applied
        // to both properties and fields.
        // For properties, the default access is any public getters/setters for the property.
        // For fields, the default access if get and set.
        // You can explicitly specify the access if you want scripts to be able to get/set a
        // property without a public getter/setter, or if you want the field property to be
        // readonly.
        // NOTE: setters are not currently supported.
        [ScriptProperty(Access = ScriptPropertyAccess.Get)]
        public long ExampleProperty => 10;

        [ScriptProperty]
        public long FieldProperty = 10;

        // An example method that can be called by scripts.
        // The method can take an IScriptRuntime as the first parameter to access the runtime
        // executing the script.
        // Other parameter types that can be marshalled include:
        // - ScriptVar (script can pass any type)
        // - long
        // - double
        // - bool
        // - string
        // - IScriptObject
        // - Any type implementing IScriptObject
        [ScriptMethod]
        public void ExampleMethod()
        {
            FieldProperty = Random.Shared.Next(0, 100);
        }

        // A constructor that creates this object. Each type can have up to one constructor.
        // The constructor can be called with the `new` keyword.
        // NOTE: This attribute cannot be applied to object constructors.
        [ScriptConstructor]
        public static ExampleObject ScriptConstructor()
        {
            return new ExampleObject();
        }

        // Called when scripts call ToString or the object is implicitly converted to a string.
        string IScriptObject.ScriptToString() => FieldProperty.ToString();

        // Implement this to do something when a reference for this object is added for the first time.
        // This is not called when additional references are added.
        void IScriptObject.ReferenceAdded(IScriptReference references)
        {

        }

        // Implement this to do something when all references to this object are removed.
        // This is not called unless all references to the object are removed.
        void IScriptObject.ReferenceRemoved(IScriptReference references)
        {

        }
    }
}
