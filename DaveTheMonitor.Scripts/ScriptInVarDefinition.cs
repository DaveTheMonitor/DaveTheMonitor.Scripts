namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptInVarDefinition
    {
        public string Identifier { get; private set; }
        public ScriptType Type { get; private set; }
        public int LocalIndex { get; private set; }

        public ScriptInVarDefinition(string id, ScriptType type, int index)
        {
            Identifier = id;
            Type = type;
            LocalIndex = index;
        }
    }
}
