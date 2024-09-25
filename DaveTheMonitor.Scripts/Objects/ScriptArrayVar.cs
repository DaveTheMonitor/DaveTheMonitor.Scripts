using DaveTheMonitor.Scripts.Attributes;
using DaveTheMonitor.Scripts.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace DaveTheMonitor.Scripts.Objects
{
    [ScriptType(Name = "array")]
    [ScriptIterator(Count = nameof(Count), GetItem = nameof(ItemAt))]
    public sealed class ScriptArrayVar : IScriptArray<ScriptArrayVar, ScriptVar>
    {
        [ScriptTypeField]
        private static ScriptType _scriptType;
        public ScriptType ScriptType => _scriptType;

        public bool IsReadOnly { get; private set; }
        public long Count => _list.Count;
        private List<ScriptVar> _list;

        public void Add(IScriptRuntime runtime, ScriptVar item)
        {
            if (IsReadOnly)
            {
                runtime.ReadOnlyError(ScriptType);
                return;
            }
            runtime.Reference.AddReference(ref item);
            _list.Add(item);
        }

        public void Insert(IScriptRuntime runtime, ScriptVar item, long index)
        {
            if (IsReadOnly)
            {
                runtime.ReadOnlyError(ScriptType);
                return;
            }
            runtime.Reference.AddReference(ref item);
            _list.Insert((int)index, item);
        }

        public bool Remove(IScriptRuntime runtime, ScriptVar item)
        {
            if (IsReadOnly)
            {
                runtime.ReadOnlyError(ScriptType);
                return false;
            }
            runtime.Reference.RemoveReference(ref item);
            return _list.Remove(item);
        }

        public void RemoveAt(IScriptRuntime runtime, long index)
        {
            if (IsReadOnly)
            {
                runtime.ReadOnlyError(ScriptType);
                return;
            }

            if (index < 0 || index >= _list.Count)
            {
                runtime.OutOfBoundsError(ScriptType, index);
                return;
            }

            runtime.Reference.RemoveReference(_list[(int)index]);
            _list.RemoveAt((int)index);
        }

        public void Clear(IScriptRuntime runtime)
        {
            if (IsReadOnly)
            {
                runtime.ReadOnlyError(ScriptType);
                return;
            }

            for (int i = 0; i < _list.Count; i++)
            {
                ScriptVar item = _list[i];
                runtime.Reference.RemoveReference(ref item);
            }

            _list.Clear();
        }

        public bool Contains(IScriptRuntime runtime, ScriptVar item)
        {
            return _list.Contains(item);
        }

        public long IndexOf(IScriptRuntime runtime, ScriptVar item)
        {
            return _list.IndexOf(item);
        }

        public ScriptVar ItemAt(IScriptRuntime runtime, long index)
        {
            if (index < 0 || index >= _list.Count)
            {
                runtime.OutOfBoundsError(ScriptType, index);
                return ScriptVar.Null;
            }
            return _list[(int)index];
        }

        public ScriptArrayVar Copy(IScriptRuntime runtime)
        {
            ScriptArrayVar arr = new ScriptArrayVar(_list.Count);
            for (int i = 0; i < _list.Count; i++)
            {
                ScriptVar item = _list[i];
                runtime.Reference.AddReference(ref item);
                arr._list.Add(item);
            }
            return arr;
        }

        public void MakeReadOnly()
        {
            IsReadOnly = true;
        }

        void IScriptObject.ReferenceAdded(IScriptReference references)
        {

        }

        void IScriptObject.ReferenceRemoved(IScriptReference references)
        {
            foreach (ScriptVar item in _list)
            {
                references.RemoveReference(item);
            }
        }

        string IScriptObject.ScriptToString() => $"array[{Count}]";

        public IEnumerator<ScriptVar> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }

        [ScriptConstructor]
        public static ScriptArrayVar Create() => new ScriptArrayVar();

        public ScriptArrayVar() : this(4)
        {

        }

        public ScriptArrayVar(int capacity)
        {
            _list = new List<ScriptVar>(capacity);
            IsReadOnly = false;
        }

        public ScriptArrayVar(IEnumerable<ScriptVar> arr, bool readOnly)
        {
            _list = new List<ScriptVar>(arr);
            IsReadOnly = readOnly;
        }

        public ScriptArrayVar(IEnumerable<long> arr, bool readOnly)
        {
            _list = new List<ScriptVar>();
            foreach (var item in arr)
            {
                _list.Add(new ScriptVar(item));
            }
            IsReadOnly = readOnly;
        }

        public ScriptArrayVar(IEnumerable<double> arr, bool readOnly)
        {
            _list = new List<ScriptVar>();
            foreach (var item in arr)
            {
                _list.Add(new ScriptVar(item));
            }
            IsReadOnly = readOnly;
        }

        public ScriptArrayVar(IEnumerable<bool> arr, bool readOnly)
        {
            _list = new List<ScriptVar>();
            foreach (var item in arr)
            {
                _list.Add(new ScriptVar(item));
            }
            IsReadOnly = readOnly;
        }

        public ScriptArrayVar(IScriptRuntime runtime, IEnumerable<string> arr, bool readOnly)
        {
            _list = new List<ScriptVar>();
            IScriptReference reference = runtime.Reference;
            foreach (var item in arr)
            {
                _list.Add(new ScriptVar(item, reference));
            }
            IsReadOnly = readOnly;
        }

        public ScriptArrayVar(IScriptRuntime runtime, IEnumerable<IScriptObject> arr, bool readOnly)
        {
            _list = new List<ScriptVar>();
            IScriptReference reference = runtime.Reference;
            foreach (var item in arr)
            {
                _list.Add(new ScriptVar(item, reference));
            }
            IsReadOnly = readOnly;
        }
    }
}
