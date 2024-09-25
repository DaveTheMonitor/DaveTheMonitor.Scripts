namespace DaveTheMonitor.Scripts.Compiler
{
    public enum CompilerOptimization
    {
        /// <summary>
        /// No optimizations will be performed on the script.
        /// </summary>
        None,
        /// <summary>
        /// Basic optimizations will be performed.
        /// </summary>
        Basic,
        /// <summary>
        /// Complex optimizations that may significantly change the bytecode will be performed.
        /// </summary>
        Aggressive
    }
}
