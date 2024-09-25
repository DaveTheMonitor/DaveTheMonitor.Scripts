using DaveTheMonitor.Scripts.Attributes;

namespace DaveTheMonitor.ScriptConsole
{
    // ScriptType here means this type contains static script functions.
    [ScriptType]
    public static class ExampleFunctions
    {
        // This is a static function, called with [GetNumber [x] [y]]
        [ScriptMethod]
        public static double Average(double x, double y)
        {
            return (x + y) / 2.0;
        }
    }
}
