namespace DaveTheMonitor.Scripts.Utilities
{
    public sealed class FastStack<T> where T : struct
    {
        public int Size { get; private set; }
        public int Count => _offset;
        private readonly T[] _stack;
        private int _offset;

        public void Push(T value)
        {
            _stack[_offset++] = value;
        }

        public void Push(ref T value)
        {
            _stack[_offset++] = value;
        }

        public T Pop()
        {
            return _stack[--_offset];
        }

        public ref T PopRef()
        {
            return ref _stack[--_offset];
        }

        public T Peek()
        {
            return _stack[_offset - 1];
        }

        public void Duplicate()
        {
            T v = _stack[_offset - 1];
            _stack[_offset++] = v;
        }

        public void Clear()
        {
            _offset = 0;
        }

        public FastStack(int size)
        {
            Size = size;
            _stack = new T[size];
            _offset = 0;
        }
    }
}
