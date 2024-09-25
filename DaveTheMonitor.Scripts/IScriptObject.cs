namespace DaveTheMonitor.Scripts
{
    public interface IScriptObject
    {
        public void ReferenceAdded(IScriptReference references)
        {

        }

        public void ReferenceRemoved(IScriptReference references)
        {

        }

        public ScriptType ScriptType => ScriptType.GetType(GetType());
        public string ScriptToString() => ScriptType.ToString();
    }
}
