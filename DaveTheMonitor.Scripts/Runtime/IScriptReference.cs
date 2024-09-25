namespace DaveTheMonitor.Scripts
{
    public interface IScriptReference
    {
        public int Size { get; }
        public int AddReference(object obj);
        public void AddReference(int reference);
        public void AddReference(ScriptVar value);
        public void AddReference(ref ScriptVar value);
        public void RemoveReference(object obj);
        public void RemoveReference(int reference);
        public void RemoveReference(ScriptVar value);
        public void RemoveReference(ref ScriptVar value);
        public int GetReference(object obj);
        public object GetObject(int reference);
        public ScriptType GetObjectType(int reference);
        public int GetReferenceCount(object obj);
        public int GetReferenceCount(int reference);
        public void ClearReferences();
    }
}
