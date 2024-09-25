using DaveTheMonitor.Scripts.Attributes;

namespace DaveTheMonitor.ScriptTests
{
    [ScriptType]
    public static class TestFunctions
    {
        [ScriptProperty]
        public static double TestProp => 15;
    }
}
