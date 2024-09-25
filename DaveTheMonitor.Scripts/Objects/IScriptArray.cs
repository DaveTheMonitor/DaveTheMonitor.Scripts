using DaveTheMonitor.Scripts.Attributes;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Objects
{
    [ScriptTypeIgnore]
    public interface IScriptArray<TSelf, TItem> : IScriptObject, IEnumerable<TItem>
    {
        [ScriptProperty]
        long Count { get; }

        [ScriptProperty]
        bool IsReadOnly { get; }

        [ScriptMethod]
        void Add(IScriptRuntime runtime, TItem value);

        [ScriptMethod]
        void Insert(IScriptRuntime runtime, TItem value, long index);

        [ScriptMethod]
        bool Remove(IScriptRuntime runtime, TItem value);

        [ScriptMethod]
        void RemoveAt(IScriptRuntime runtime, long index);

        [ScriptMethod]
        void Clear(IScriptRuntime runtime);

        [ScriptMethod]
        bool Contains(IScriptRuntime runtime, TItem item);

        [ScriptMethod]
        long IndexOf(IScriptRuntime runtime, TItem item);

        [ScriptMethod]
        TItem ItemAt(IScriptRuntime runtime, long index);

        [ScriptMethod]
        TSelf Copy(IScriptRuntime runtime);
    }
}
