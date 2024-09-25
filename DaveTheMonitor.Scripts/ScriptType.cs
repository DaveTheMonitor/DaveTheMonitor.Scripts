using DaveTheMonitor.Scripts.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptType
    {
        // const is private so if it changes other assemblies
        // using the property will continue working
        internal const string _scriptsNamespace = "scripts";
        public static string ScriptsNamespace => "scripts";
        public static ScriptType Void { get; private set; }
        public static ScriptType ScriptVar { get; private set; }
        public static ScriptType Double { get; private set; }
        public static ScriptType Long { get; private set; }
        public static ScriptType Bool { get; private set; }
        public static ScriptType String { get; private set; }
        public static ScriptType IScriptObject { get; private set; }
        public ScriptType BaseType { get; private set; }
        public ScriptType[] Interfaces { get; private set; }
        public Type Type { get; private set; }
        public int Id { get; private set; }
        public string Namespace { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public ScriptMethod[] Methods { get; private set; }
        public ScriptProperty[] Properties { get; private set; }
        public bool Nullable { get; private set; }
        public bool CanIterate { get; private set; }
        public bool MaybeRef { get; private set; }
        public ScriptMethod IteratorGetItem { get; private set; }
        public ScriptMember IteratorCount { get; private set; }
        public ScriptMethod Ctor { get; private set; }
        private static ScriptType[] _typesArray;
        private static int _ids;
#if NET8_0_OR_GREATER
        private static FrozenDictionary<Type, ScriptType> _types;
        private static FrozenDictionary<string, ScriptType> _typesNamed;
        private FrozenDictionary<string, ScriptMethod> _methodsDictionary;
        private FrozenDictionary<string, ScriptProperty> _propertiesDictionary;
#else
        private static Dictionary<Type, ScriptType> _types;
        private static Dictionary<string, ScriptType> _typesNamed;
        private Dictionary<string, ScriptMethod> _methodsDictionary;
        private Dictionary<string, ScriptProperty> _propertiesDictionary;
#endif
        private bool _methodsInitialized;

        static ScriptType()
        {
            _types = null;
        }

        public static ScriptType GetType(IScriptObject obj)
        {
            return obj.ScriptType;
        }

        public static ScriptType GetType(int id)
        {
            return _typesArray[id];
        }

        public static ScriptType GetType(string fullName)
        {
            _typesNamed.TryGetValue(fullName, out ScriptType t);
            return t;
        }

        public static ScriptType GetType(string @namespace, string name)
        {
            return GetType($"{@namespace}.{name}");
        }

        public static ScriptType GetType(Type type)
        {
            _types.TryGetValue(type, out ScriptType scriptType);
            return scriptType;
        }

        public static List<ScriptType> GetTypes(List<string> namespaces, string name)
        {
            List<ScriptType> l = new List<ScriptType>();
            foreach (ScriptType type in _typesArray)
            {
                if (type.Name == name && namespaces.Contains(type.Namespace))
                {
                    l.Add(type);
                }
            }
            return l;
        }

        public static ScriptType[] GetAllTypes()
        {
            return _typesArray;
        }

        public static string GetDefaultNamespace(Type type)
        {
            Assembly asm = type.Assembly;
            ScriptAssemblyNamespaceAttribute attribute = asm.GetCustomAttribute<ScriptAssemblyNamespaceAttribute>();
            if (attribute != null)
            {
                return attribute.Namespace.ToLowerInvariant();
            }
            else
            {
                return asm.GetName().Name.ToLowerInvariant();
            }
        }

        public static bool IsTypeOrSubType(IScriptReference reference, ScriptVar value, ScriptType type)
        {
            if (type.Id == Void.Id)
            {
                throw new ArgumentException("type cannot be void", nameof(type));
            }
            else if (type.Id == ScriptVar.Id)
            {
                return true;
            }
            else if (type.Id == Double.Id)
            {
                return value.Type == ScriptVarType.Double;
            }
            else if (type.Id == Long.Id)
            {
                return value.Type == ScriptVarType.Double && value.GetDoubleValue() == (long)value.GetDoubleValue();
            }
            else if (type.Id == Bool.Id)
            {
                return value.Type == ScriptVarType.Bool;
            }
            else if (type.Id == String.Id)
            {
                return value.Type == ScriptVarType.String;
            }
            else if (type.Id == IScriptObject.Id)
            {
                return value.IsRef;
            }
            else
            {
                return value.GetObjectValue(reference).ScriptType.IsTypeOrSubType(type);
            }
        }

        public static void RegisterTypes(IEnumerable<Assembly> assemblies)
        {
            Type iScriptObject = typeof(IScriptObject);
            List<ScriptType> typesList = new List<ScriptType>();
            List<Type> staticMethodTypes = new List<Type>();
            Dictionary<ScriptType, Type> inheritedTypes = new Dictionary<ScriptType, Type>();
            Dictionary<Type, ScriptType> types = new Dictionary<Type, ScriptType>();
            Dictionary<string, FieldInfo> constants = new Dictionary<string, FieldInfo>();
            _ids = 0;

            Void = CreateType(typeof(void), false);
            typesList.Add(Void);
            ScriptVar = CreateType(typeof(ScriptVar), false);
            typesList.Add(ScriptVar);
            Double = CreateType(typeof(double), false);
            typesList.Add(Double);
            Long = CreateType(typeof(long), false);
            typesList.Add(Long);
            Bool = CreateType(typeof(bool), false);
            typesList.Add(Bool);
            String = CreateType(typeof(string), false);
            typesList.Add(String);
            IScriptObject = CreateType(typeof(IScriptObject), false);
            typesList.Add(IScriptObject);

            HashSet<string> loaded = new HashSet<string>();

            Assembly scriptsAssembly = Assembly.GetExecutingAssembly();
            loaded.Add(scriptsAssembly.FullName);
            RegisterAssembly(scriptsAssembly, typesList, inheritedTypes, staticMethodTypes);

            foreach (Assembly assembly in assemblies)
            {
                if (loaded.Add(assembly.FullName))
                {
                    RegisterAssembly(assembly, typesList, inheritedTypes, staticMethodTypes);
                }
            }
            
            foreach (ScriptType t in typesList)
            {
                types.Add(t.Type, t);
            }

#if NET8_0_OR_GREATER
            _types = types.ToFrozenDictionary();
            _typesNamed = types.ToFrozenDictionary(p => p.Value.Namespace + '.' + p.Value.Name, p => p.Value);
#else
            _types = types;
            _typesNamed = types.ToDictionary(p => p.Value.Namespace + '.' + p.Value.Name, p => p.Value);
#endif

            _typesArray = typesList.ToArray();

            foreach (Type type in staticMethodTypes)
            {
                HashSet<string> names = new HashSet<string>();
                CreateMethods(null, type, names);
                CreateProperties(null, type, names);
                CreateConstants(type, names);
            }

            // InitMembers must be called after all types are registered
            // otherwise it could throw errors for arg/return types that
            // are valid but haven't been registered yet
            foreach (ScriptType t in _typesArray)
            {
                t.InitMembers();
            }
        }

        private static void RegisterAssembly(Assembly assembly, List<ScriptType> typesList, Dictionary<ScriptType, Type> inheritedTypes, List<Type> staticMethodTypes)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ScriptTypeIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (AnyBaseImplementsScriptObject(type))
                {
                    RegisterType(type, typesList, inheritedTypes);
                }
                else if (type.GetCustomAttribute<ScriptTypeAttribute>() != null)
                {
                    staticMethodTypes.Add(type);
                }
            }
        }

        private static ScriptType RegisterType(Type type, List<ScriptType> typesList, Dictionary<ScriptType, Type> inheritedTypes)
        {
            foreach (ScriptType s in typesList)
            {
                if (s.Type == type)
                {
                    return s;
                }
            }

            bool anyBaseImplementsScriptObject = false;
            ScriptType baseType = null;
            if (type.BaseType != null && AnyBaseImplementsScriptObject(type.BaseType))
            {
                anyBaseImplementsScriptObject = true;
                baseType = RegisterType(type.BaseType, typesList, inheritedTypes);
            }
            List<ScriptType> interfaces = new List<ScriptType>();
            foreach (Type @interface in type.GetInterfaces())
            {
                if (@interface.GetCustomAttribute<ScriptTypeIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (!@interface.GetInterfaces().Contains(typeof(IScriptObject)))
                {
                    continue;
                }

                interfaces.Add(RegisterType(@interface, typesList, inheritedTypes));
            }

            ScriptType scriptType = CreateType(type, anyBaseImplementsScriptObject);
            scriptType.BaseType = baseType;
            scriptType.Interfaces = interfaces.ToArray();

            if (typesList.Count > scriptType.Id)
            {
                typesList[scriptType.Id] = scriptType;
            }
            else
            {
                typesList.Add(scriptType);
            }

            FieldInfo scriptTypeField = null;
            foreach (FieldInfo field in type.GetFields())
            {
                if (!field.IsStatic)
                {
                    continue;
                }

                if (field.GetCustomAttribute<ScriptTypeFieldAttribute>() != null)
                {
                    scriptTypeField = field;
                    break;
                }
                else if (field.Name == "ScriptType")
                {
                    scriptTypeField = field;
                }
            }
            scriptTypeField?.SetValue(null, scriptType);

            return scriptType;
        }

        private static bool AnyBaseImplementsScriptObject(Type type)
        {
            while (type != null)
            {
                if (type.GetInterfaces().Contains(typeof(IScriptObject)))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        private static ScriptType CreateType(Type type, bool baseTypeImplementsScriptObject)
        {
            ScriptType scriptType = new ScriptType(type, baseTypeImplementsScriptObject);
            scriptType.Id = _ids++;
            return scriptType;
        }

        private static void ThrowIfNotValid(MethodInfo method)
        {
            if (method.IsGenericMethod)
            {
                throw new Exception("ScriptMethod cannot be generic. Call a generic method from within the ScriptMethod.");
            }
            else if (method.IsConstructor)
            {
                throw new Exception("ScriptMethod cannot be constructor.");
            }
        }

        private static void ThrowIfNotValid(ParameterInfo param)
        {
            if (param.IsIn)
            {
                throw new Exception("ScriptMethod param cannot be in.");
            }
            else if (param.IsOut)
            {
                throw new Exception("ScriptMethod param cannot be out.");
            }
            else if (param.ParameterType.IsByRef)
            {
                throw new Exception("ScriptMethod param cannot be ref.");
            }
            else if (param.HasDefaultValue)
            {
                throw new Exception("ScriptMethod param cannot have a default value.");
            }
            else if (param.IsOptional)
            {
                throw new Exception("ScriptMethod param cannot be optional.");
            }
        }

        private static void ThrowIfNotValid(PropertyInfo property)
        {
            MethodInfo method = property.GetMethod ?? property.SetMethod;
            if (method.IsGenericMethod)
            {
                throw new Exception("ScriptProperty cannot be generic.");
            }
        }

        private static List<ScriptMethod> CreateMethods(ScriptType scriptType, Type type, HashSet<string> names)
        {
            if (!MustImplementIScriptObject(type))
            {
                return new List<ScriptMethod>();
            }

            List<ScriptMethod> list = new List<ScriptMethod>();
            Type[] interfaces = null;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                ScriptMethodAttribute attribute = method.GetCustomAttribute<ScriptMethodAttribute>();

                bool skip = false;
                string name = null;
                if (attribute == null)
                {
                    skip = true;
                    if (type.IsInterface)
                    {
                        continue;
                    }

                    interfaces ??= type.GetInterfaces();
                    foreach (Type @interface in interfaces)
                    {
                        InterfaceMapping map = type.GetInterfaceMap(@interface);
                        int i = Array.IndexOf(map.TargetMethods, method);
                        if (i == -1)
                        {
                            break;
                        }

                        MethodInfo @base = map.InterfaceMethods[i];
                        ScriptMethodAttribute baseAttribute = @base.GetCustomAttribute<ScriptMethodAttribute>();
                        if (baseAttribute == null)
                        {
                            break;
                        }

                        name = (baseAttribute.Name ?? @base.Name).ToLowerInvariant();
                        skip = false;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                ThrowIfNotValid(method);
                if (scriptType == null && !method.IsStatic)
                {
                    throw new Exception($"Non-static ScriptMethod {method.DeclaringType.FullName}.{method.Name} used for non-script type {method.DeclaringType.FullName}.");
                }

                name ??= (attribute?.Name ?? method.Name).ToLowerInvariant();
                if (names.Contains(name))
                {
                    throw new Exception($"Duplicate ScriptMember {name} on {method.DeclaringType.FullName}.");
                }

                names.Add(name);

                ScriptType returnType;
                if (method.ReturnType == typeof(void))
                {
                    returnType = null;
                }
                else
                {
                    returnType = GetType(method.ReturnType);
                    if (returnType == null)
                    {
                        throw new Exception($"ScriptMethod {method.DeclaringType.FullName}.{method.Name} has invalid return type ({method.ReturnType.FullName})");
                    }
                }

                ScriptType[] args = null;
                ParameterInfo[] @params = method.GetParameters();
                if (@params.Length != 0)
                {
                    bool runtimeArg = @params[0].ParameterType == typeof(IScriptRuntime);
                    args = new ScriptType[runtimeArg ? @params.Length - 1 : @params.Length];
                    for (int i = 0; i < @params.Length; i++)
                    {
                        if (runtimeArg && i == 0)
                        {
                            continue;
                        }
                        ParameterInfo param = @params[i];
                        ThrowIfNotValid(param);
                        if (param.ParameterType == typeof(IScriptRuntime))
                        {
                            throw new Exception($"ScriptMethod {method.DeclaringType.FullName}.{method.Name} IScriptRuntime param must be the param.");
                        }
                        ScriptType paramType = GetType(param.ParameterType);
                        if (paramType == null)
                        {
                            throw new Exception($"ScriptMethod {method.DeclaringType.FullName}.{method.Name} has invalid param type ({param.ParameterType.FullName}).");
                        }
                        args[runtimeArg ? i - 1 : i] = paramType;
                    }
                }

                ScriptMethod scriptMethod = ScriptMethod.Create(scriptType, method, returnType, args, name);
                list.Add(scriptMethod);
            }
            return list;
        }

        private static ScriptMethod CreateCtor(ScriptType scriptType, Type type)
        {
            if (!MustImplementIScriptObject(type))
            {
                return null;
            }

            ScriptMethod ctor = null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                ScriptConstructorAttribute attribute = method.GetCustomAttribute<ScriptConstructorAttribute>();
                if (attribute != null)
                {
                    if (ctor != null)
                    {
                        throw new Exception($"ScriptType {type.FullName} can only specify one constructor.");
                    }

                    if (method.GetCustomAttribute<ScriptMethodAttribute>() != null)
                    {
                        throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} cannot specify ScriptMethodAttribute.");
                    }

                    ThrowIfNotValid(method);
                    if (!method.IsStatic)
                    {
                        throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} must be static.");
                    }

                    if (scriptType == null)
                    {
                        throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} must be declared on a valid script type.");
                    }

                    if (method.ReturnType != type)
                    {
                        throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} must return declaring type.");
                    }

                    ScriptType[] args = null;
                    ParameterInfo[] @params = method.GetParameters();
                    if (@params.Length != 0)
                    {
                        bool runtimeArg = @params[0].ParameterType == typeof(IScriptRuntime);
                        args = new ScriptType[runtimeArg ? @params.Length - 1 : @params.Length];
                        for (int i = 0; i < @params.Length; i++)
                        {
                            if (runtimeArg && i == 0)
                            {
                                continue;
                            }
                            ParameterInfo param = @params[i];
                            ThrowIfNotValid(param);
                            if (param.ParameterType == typeof(IScriptRuntime))
                            {
                                throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} IScriptRuntime param must be the param.");
                            }
                            ScriptType paramType = GetType(param.ParameterType);
                            if (paramType == null)
                            {
                                throw new Exception($"ScriptConstructor {type.FullName}.{method.Name} has invalid param type ({param.ParameterType.FullName}).");
                            }
                            args[runtimeArg ? i - 1 : i] = paramType;
                        }
                    }

                    ctor = ScriptMethod.Create(scriptType, method, scriptType, args, "ctor");
                }
            }

            return ctor;
        }

        private static List<ScriptProperty> CreateProperties(ScriptType scriptType, Type type, HashSet<string> names)
        {
            if (!MustImplementIScriptObject(type))
            {
                return new List<ScriptProperty>();
            }

            List<ScriptProperty> list = new List<ScriptProperty>();
            Type[] interfaces = null;

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                ScriptPropertyAttribute attribute = property.GetCustomAttribute<ScriptPropertyAttribute>();
                bool skip = false;
                string name = null;
                ScriptPropertyAccess? access = null;

                if (attribute == null)
                {
                    skip = true;
                    interfaces ??= type.GetInterfaces();
                    foreach (Type @interface in interfaces)
                    {
                        InterfaceMapping map = type.GetInterfaceMap(@interface);
                        int i = Array.IndexOf(map.TargetMethods, property.GetMethod);
                        if (i == -1)
                        {
                            break;
                        }

                        MethodInfo @base = map.InterfaceMethods[i];
                        PropertyInfo baseProp = @interface.GetProperty(@base.Name.Substring(@base.Name.IndexOf('_') + 1), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (baseProp == null || (baseProp.GetMethod != @base && baseProp.SetMethod != @base))
                        {
                            break;
                        }

                        ScriptPropertyAttribute baseAttribute = @baseProp.GetCustomAttribute<ScriptPropertyAttribute>();
                        if (baseAttribute == null)
                        {
                            break;
                        }

                        name = (baseAttribute.Name ?? @baseProp.Name).ToLowerInvariant();
                        access = baseAttribute.Access;
                        skip = false;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                if (scriptType == null && !(property.GetMethod?.IsStatic ?? property.SetMethod.IsStatic))
                {
                    throw new Exception($"Non-static ScriptProperty {property.DeclaringType.FullName}.{property.Name} used for non-script type {type.FullName}.");
                }

                name ??= (attribute.Name ?? property.Name).ToLowerInvariant();
                access ??= attribute.Access;
                if (names.Contains(name))
                {
                    throw new Exception($"Duplicate ScriptMember {name} on {type.FullName}.");
                }
                names.Add(name);

                ScriptType propertyType = GetType(property.PropertyType);
                if (propertyType == null)
                {
                    throw new Exception($"Invalid ScriptProperty {type.FullName}.{property.Name} type ({type.FullName})");
                }
                ThrowIfNotValid(property);

                ScriptProperty scriptProperty = ScriptProperty.Create(scriptType, property, propertyType, access.Value, name);
                list.Add(scriptProperty);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                ScriptPropertyAttribute attribute = field.GetCustomAttribute<ScriptPropertyAttribute>();
                if (attribute != null)
                {
                    if (scriptType == null && !field.IsStatic)
                    {
                        throw new Exception($"Non-static ScriptProperty {type.FullName}.{field.Name} used for non-script type {type.Name}.");
                    }
                    string name = (attribute.Name ?? field.Name).ToLowerInvariant();
                    if (names.Contains(name))
                    {
                        throw new Exception($"Duplicate ScriptMember {name} on type {type.FullName}");
                    }
                    names.Add(name);

                    ScriptType propertyType = GetType(field.FieldType);
                    if (propertyType == null)
                    {
                        throw new Exception($"Invalid ScriptProperty {type.FullName}.{field.Name} type ({type.FullName})");
                    }

                    ScriptProperty scriptProperty = ScriptProperty.Create(scriptType, field, propertyType, attribute.Access);
                    list.Add(scriptProperty);
                }
            }
            return list;
        }

        private static List<ScriptConst> CreateConstants(Type type, HashSet<string> names)
        {
            List<ScriptConst> list = new List<ScriptConst>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                ScriptConstAttribute attribute = field.GetCustomAttribute<ScriptConstAttribute>();
                if (attribute != null)
                {
                    if (!field.IsLiteral)
                    {
                        throw new Exception($"ScriptConst {type.FullName}.{field.Name} must be constant.");
                    }

                    string name = (attribute.Name ?? field.Name).ToLowerInvariant();
                    if (names.Contains(name))
                    {
                        throw new Exception($"Duplicate ScriptMember {name} on type {type.FullName}");
                    }
                    names.Add(name);

                    Type fieldType = field.FieldType;

                    ScriptType constType;
                    object v;
                    if (fieldType == typeof(double))
                    {
                        v = (double)field.GetRawConstantValue();
                        constType = Double;
                    }
                    else if (fieldType == typeof(long))
                    {
                        v = (long)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(int))
                    {
                        v = (long)(int)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(uint))
                    {
                        v = (long)(uint)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(byte))
                    {
                        v = (long)(byte)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(sbyte))
                    {
                        v = (long)(sbyte)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(short))
                    {
                        v = (long)(short)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(ushort))
                    {
                        v = (long)(ushort)field.GetRawConstantValue();
                        constType = Long;
                    }
                    else if (fieldType == typeof(bool))
                    {
                        v = (bool)field.GetRawConstantValue();
                        constType = Bool;
                    }
                    else if (fieldType == typeof(string))
                    {
                        v = (string)field.GetRawConstantValue();
                        constType = String;
                    }
                    else
                    {
                        throw new Exception($"Invalid ScriptConst {type.FullName}.{field.Name} type ({type.FullName})");
                    }

                    list.Add(ScriptConst.Create(field, constType, v));
                }
            }
            return list;
        }

        public ScriptMethod GetMethod(string name)
        {
            if (_methodsDictionary != null && _methodsDictionary.TryGetValue(name, out ScriptMethod method))
            {
                return method;
            }

            method = BaseType?.GetMethod(name);
            if (method != null)
            {
                return method;
            }

            foreach (ScriptType @interface in Interfaces)
            {
                method = @interface.GetMethod(name);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        public ScriptMethod GetMethod(MethodInfo method)
        {
            foreach (ScriptMethod m in Methods)
            {
                if (m.Method == method)
                {
                    return m;
                }
            }

            ScriptMethod scriptMethod = BaseType?.GetMethod(method);
            if (scriptMethod != null)
            {
                return scriptMethod;
            }

            foreach (ScriptType @interface in Interfaces)
            {
                scriptMethod = @interface.GetMethod(method);
                if (scriptMethod != null)
                {
                    return scriptMethod;
                }
            }

            return null;
        }

        public ScriptProperty GetProperty(string name)
        {
            if (_propertiesDictionary != null && _propertiesDictionary.TryGetValue(name, out ScriptProperty property))
            {
                return property;
            }

            property = BaseType?.GetProperty(name);
            if (property != null)
            {
                return property;
            }

            foreach (ScriptType @interface in Interfaces)
            {
                property = @interface.GetProperty(name);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }

        public ScriptProperty GetProperty(MemberInfo member)
        {
            foreach (ScriptProperty p in Properties)
            {
                if (p.Member == member)
                {
                    return p;
                }
            }

            ScriptProperty scriptProperty = BaseType?.GetProperty(member);
            if (scriptProperty != null)
            {
                return scriptProperty;
            }

            foreach (ScriptType @interface in Interfaces)
            {
                scriptProperty = @interface.GetProperty(member);
                if (scriptProperty != null)
                {
                    return scriptProperty;
                }
            }

            return null;
        }

        public ScriptMember GetMember(string name)
        {
            ScriptMember member = GetMethod(name);
            return member ?? GetProperty(name);
        }

        public bool IsTypeOrSubType(ScriptType type)
        {
            if (type.Id == Id)
            {
                return true;
            }
            ScriptType baseType = BaseType;
            while (baseType != null)
            {
                if (baseType.Id == type.Id)
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            if (Interfaces.Contains(type))
            {
                return true;
            }
            return false;
        }

        private void InitMembers()
        {
            if (_methodsInitialized)
            {
                return;
            }
            _methodsInitialized = true;

            HashSet<string> names = new HashSet<string>();

            if (BaseType != null)
            {
                // Base type must be initialized first so we can detect overrides
                // It may not be initialized yet depending on the type load order
                BaseType.InitMembers();
                foreach (ScriptMethod method in BaseType.Methods)
                {
                    names.Add(method.Name);
                }
                foreach (ScriptProperty property in BaseType.Properties)
                {
                    names.Add(property.Name);
                }
            }

            names.Clear();

            Ctor = CreateCtor(this, Type);
            List<ScriptMethod> scriptMethods = CreateMethods(this, Type, names);
            List<ScriptProperty> scriptProperties = CreateProperties(this, Type, names);
            List<ScriptConst> scriptConstants = CreateConstants(Type, names);

            if (scriptMethods.Count > 0)
            {
                Methods = scriptMethods.ToArray();
#if NET8_0_OR_GREATER
                _methodsDictionary = scriptMethods.ToFrozenDictionary(m => m.Name);
#else
                _methodsDictionary = scriptMethods.ToDictionary(m => m.Name);
#endif
            }
            else
            {
                Methods = Array.Empty<ScriptMethod>();
                _methodsDictionary = null;
            }

            if (scriptProperties.Count > 0)
            {
                Properties = scriptProperties.ToArray();
#if NET8_0_OR_GREATER
                _propertiesDictionary = scriptProperties.ToFrozenDictionary(p => p.Name);
#else
                _propertiesDictionary = scriptProperties.ToDictionary(p => p.Name);
#endif
            }
            else
            {
                Properties = Array.Empty<ScriptProperty>();
            }

            ScriptIteratorAttribute attribute = Type.GetCustomAttribute<ScriptIteratorAttribute>();
            if (attribute != null)
            {
                CanIterate = true;
                if (attribute.Count == null)
                {
                    throw new Exception($"{Type.FullName} ScriptIteratorAttribute.Count cannot be null");
                }
                else if (attribute.GetItem == null)
                {
                    throw new Exception($"{Type.FullName} ScriptIteratorAttribute.GetItem cannot be null");
                }

                ScriptMember countMember = GetMember(attribute.Count.ToLowerInvariant());
                if (countMember == null)
                {
                    throw new Exception($"{Type.FullName} iterator Count not found.");
                }

                if (countMember is ScriptMethod countMethod)
                {
                    if (countMethod.ReturnType != Long)
                    {
                        throw new Exception($"{Type.FullName} {countMember.Name} iterator Count ScriptMethod must return a long.");
                    }
                    else if (countMethod.Args.Length != 0)
                    {
                        throw new Exception($"{Type.FullName} {countMember.Name} iterator Count ScriptMethod must take no params.");
                    }
                }
                else if (countMember is ScriptProperty countProperty)
                {
                    if (countProperty == null)
                    {
                        throw new Exception($"{Type.FullName} {countMember.Name} iterator Count is not a valid ScriptProperty.");
                    }
                    else if (!countProperty.CanGet)
                    {
                        throw new Exception($"{Type.FullName} {countMember.Name} iterator Count ScriptProperty must have a getter.");
                    }
                    else if (countProperty.PropertyType != Long)
                    {
                        throw new Exception($"{Type.FullName} {countMember.Name} iterator Count ScriptProperty must return a long.");
                    }
                }
                else
                {
                    throw new Exception($"{Type.FullName} {attribute.Count} iterator Count must be a ScriptMethod or ScriptProperty.");
                }

                IteratorCount = countMember;

                IteratorGetItem = GetMethod(attribute.GetItem.ToLowerInvariant());
                if (IteratorGetItem == null)
                {
                    throw new Exception($"{Type.FullName} iterator GetItem not found.");
                }

                if (IteratorGetItem.Args.Length != 1 || IteratorGetItem.Args[0] != Long)
                {
                    throw new Exception($"{Type.FullName} {countMember.Name} iterator GetItem ScriptMethod must take only one long param.");
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        private static bool MustImplementIScriptObject(Type type)
        {
            return type != typeof(void)
                && type != typeof(ScriptVar)
                && type != typeof(double)
                && type != typeof(long)
                && type != typeof(bool)
                && type != typeof(string)
                && type != typeof(IScriptObject);
        }

        private ScriptType(Type type, bool baseTypeImplementsScriptObject)
        {
            Type = type;
            Interfaces = Array.Empty<ScriptType>();
            if (type == typeof(double))
            {
                Namespace = "scripts";
                Name = "number";
                Nullable = false;
                MaybeRef = false;
            }
            else if (type == typeof(long))
            {
                Namespace = "scripts";
                Name = "int";
                Nullable = false;
                MaybeRef = false;
            }
            else if (type == typeof(bool))
            {
                Namespace = "scripts";
                Name = "bool";
                Nullable = false;
                MaybeRef = false;
            }
            else if (type == typeof(ScriptVar))
            {
                Namespace = "scripts";
                Name = "dynamic";
                Nullable = true;
                MaybeRef = true;
            }
            else if (type == typeof(string))
            {
                Namespace = "scripts";
                Name = "string";
                Nullable = true;
                MaybeRef = true;
            }
            else if (type == typeof(IScriptObject))
            {
                Namespace = "scripts";
                Name = "object";
                Nullable = true;
                MaybeRef = true;
            }
            else if (type == typeof(void))
            {
                Namespace = "scripts";
                Name = "void";
                Nullable = false;
                MaybeRef = false;
            }
            else
            {
                ScriptTypeAttribute attribute = type.GetCustomAttribute<ScriptTypeAttribute>(false);
                Name = (attribute?.Name ?? type.Name).ToLowerInvariant();
                Namespace = attribute?.Namespace?.ToLowerInvariant() ?? GetDefaultNamespace(type);
                Nullable = true;
                MaybeRef = true;
            }
            FullName = Namespace + '.' + Name;
            _methodsInitialized = false;

            // These special script types don't have any methods associated with them
            // and shouldn't be tested for IScriptObject implementation
            if (!MustImplementIScriptObject(Type))
            {
                return;
            }

            // Types with a base type that implements IScriptObject shouldn't be
            // required to implement it themselves
            if (baseTypeImplementsScriptObject)
            {
                return;
            }

            if (!Type.GetInterfaces().Contains(typeof(IScriptObject)))
            {
                throw new Exception($"ScriptType {type.FullName} must implement IScriptObject");
            }
        }
    }
}
