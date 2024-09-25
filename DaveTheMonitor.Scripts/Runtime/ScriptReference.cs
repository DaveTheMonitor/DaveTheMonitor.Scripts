using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DaveTheMonitor.Scripts
{
    public sealed class ScriptReference : IScriptReference
    {
        private struct Reference
        {
            public object Object;
            public ScriptVarType Type;
            public int Count;

            public override string ToString()
            {
                return Object != null ? $"{Object.GetType().Name}:{Count}" : "Empty";
            }
        }

        public int Size => _refs.Length;
        private Reference[] _refs;
        private Dictionary<object, int> _refsDictionary;
        private int[] _free;
        private int _freeOffset;
        private int _used;

        public int AddReference(object obj)
        {
            int i = -1;
            if (_refsDictionary.TryGetValue(obj, out i))
            {
                _refs[i].Count++;
            }
            else
            {
                i = _free[--_freeOffset];
                ref Reference r = ref _refs[i];
                r.Object = obj;
                if (obj is IScriptObject sobj)
                {
                    sobj.ReferenceAdded(this);
                    r.Type = ScriptVarType.Object;
                }
                else
                {
                    r.Type = ScriptVarType.String;
                }
                r.Count = 1;
                _refsDictionary[obj] = i;
                _used++;
            }
            return i;
        }

        public void AddReference(int reference)
        {
            _refs[reference].Count++;
        }

        public void AddReference(ref ScriptVar value)
        {
            if (value.IsRef)
            {
                AddReference(value.GetObjectId());
            }
        }

        public void AddReference(ScriptVar value)
        {
            if (value.IsRef)
            {
                AddReference(value.GetObjectId());
            }
        }

        public object GetObject(int reference)
        {
            return _refs[reference].Object;
        }

        public ScriptType GetObjectType(int reference)
        {
            object obj = _refs[reference].Object;
            return (obj is string) ? ScriptType.GetType(5) : ((IScriptObject)obj).ScriptType;
        }

        public int GetReference(object obj)
        {
            if (_refsDictionary.TryGetValue(obj, out int i))
            {
                return i;
            }
            return -1;
        }

        public void RemoveReference(object obj)
        {
            if (_refsDictionary.TryGetValue(obj, out int i))
            {
                RemoveReference(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReference(int reference)
        {
            ref Reference r = ref _refs[reference];
            if (--r.Count == 0)
            {
                _refsDictionary.Remove(r.Object);
                _free[_freeOffset++] = reference;
                _used--;

                if (r.Type == ScriptVarType.Object)
                {
                    ((IScriptObject)r.Object).ReferenceRemoved(this);
                }
            }
        }

        public void RemoveReference(ScriptVar value)
        {
            if (value.IsRef)
            {
                RemoveReference(value.GetObjectId());
            }
        }

        public void RemoveReference(ref ScriptVar value)
        {
            if (value.IsRef)
            {
                RemoveReference(value.GetObjectId());
            }
        }

        public int GetReferenceCount(object obj)
        {
            if (_refsDictionary.TryGetValue(obj, out int i))
            {
                return _refs[i].Count;
            }
            return 0;
        }

        public int GetReferenceCount(int reference)
        {
            return _refs[reference].Count;
        }

        public void ClearReferences()
        {
            _refsDictionary.Clear();
            if (_used == 0)
            {
                return;
            }

            for (int i = 0; i < _refs.Length; i++)
            {
                _refs[i].Count = 0;
                _free[i] = i;
            }
            _freeOffset = _free.Length;
            _used = 0;
        }

        public void WarnNotRemovedReferences(IScriptRuntime runtime)
        {
            if (_used == 0)
            {
                return;
            }

            for (int i = 0; i < _refs.Length; i++)
            {
                int count = GetReferenceCount(i);
                if (count > 0)
                {
                    runtime.Warn(ScriptErrorCode.R_ReferenceNotRemoved, "Reference Not Removed", $"Reference {i} not removed: {count} references remaining. If no other errors were thrown, this is most likely a runtime issue.");
                }
            }
        }

        public ScriptReference(int size)
        {
            _refs = new Reference[size];
            _refsDictionary = new Dictionary<object, int>(size);
            _free = new int[size];
            for (int i = 0; i < size; i++)
            {
                _free[i] = i;
            }
            _freeOffset = _free.Length;
            _used = 0;
        }
    }
}
