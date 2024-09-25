using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaveTheMonitor.Scripts.Utilities
{
    public unsafe sealed class UnsafeStack<T> : IDisposable where T : unmanaged
    {
        public int Size { get; private set; }
        public int Count => (int)((_ptr - _start) / sizeof(T));
        public bool HasSpace => _ptr < _end;
        public bool Empty => _ptr <= _start;
        public bool NotEmpty => _ptr > _start;
        private nint _start;
        private nint _end;
        private nint _ptr;
        private bool _disposedValue;

        public void Push(T value)
        {
            Unsafe.WriteUnaligned((void*)_ptr, value);
            _ptr += sizeof(T);
        }

        public T Pop()
        {
            _ptr -= sizeof(T);
            T value = Unsafe.ReadUnaligned<T>((void*)_ptr);
            return value;
        }

        public T Peek()
        {
            return Unsafe.ReadUnaligned<T>((void*)(_ptr - sizeof(T)));
        }

        public void Duplicate()
        {
            T v = Unsafe.ReadUnaligned<T>((void*)(_ptr - sizeof(T)));
            Unsafe.WriteUnaligned((void*)_ptr, v);
            _ptr += sizeof(T);
        }

        public void Clear()
        {
            _ptr = _start;
        }

        public UnsafeStack(int size)
        {
            Size = size;
            _start = Marshal.AllocHGlobal(sizeof(T) * size);
            _end = _start + (sizeof(T) * size);
            _ptr = _start;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                Marshal.FreeHGlobal(_start);
                _disposedValue = true;
            }
        }

        ~UnsafeStack()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
