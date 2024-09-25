namespace DaveTheMonitor.Scripts
{
    public abstract class ScriptMember
    {
        public ScriptType DeclaringType { get; protected set; }
        public string Namespace { get; protected set; }
        public string Name { get; protected set; }
        public int Id { get; protected set; }
        public bool IsStatic { get; protected set; }
        public bool IsCtor { get; protected set; }
    }
}
