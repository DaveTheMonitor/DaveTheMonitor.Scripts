using DaveTheMonitor.Scripts.Attributes;
using System;

namespace DaveTheMonitor.Scripts.Objects
{
    [ScriptType(Name = "random")]
    public sealed class ScriptRandom : IScriptObject
    {
        [ScriptTypeField]
        private static ScriptType _scriptType;
        public ScriptType ScriptType => _scriptType;

        [ScriptProperty]
        public long Seed { get; private set; }
        private Random _random;

        [ScriptMethod]
        public long NextInt(IScriptRuntime runtime, long min, long max)
        {
            if (min >= max)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid Random Arguments", "min must be < max");
                return 0;
            }
            return _random.NextInt64(min, max);
        }

        [ScriptMethod]
        public double Next(IScriptRuntime runtime, double min, double max)
        {
            if (min >= max)
            {
                runtime.Error(ScriptErrorCode.R_InvalidArg, "Invalid Random Arguments", "min must be < max");
                return 0;
            }
            return min + (_random.NextDouble() * (max - min));
        }

        void IScriptObject.ReferenceAdded(IScriptReference references)
        {
            
        }

        void IScriptObject.ReferenceRemoved(IScriptReference references)
        {

        }

        string IScriptObject.ScriptToString() => $"random[{Seed}]";

        [ScriptConstructor]
        public static ScriptRandom Create(long seed)
        {
            if (seed == 0)
            {
                seed = DefaultScriptFunctions.Rand.Next(int.MinValue, int.MaxValue);
            }
            return new ScriptRandom((int)seed);
        }

        public ScriptRandom(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }
    }
}
