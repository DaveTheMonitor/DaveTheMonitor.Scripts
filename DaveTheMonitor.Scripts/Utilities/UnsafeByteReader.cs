using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DaveTheMonitor.Scripts.Utilities
{
    public unsafe sealed class UnsafeByteReader : IDisposable
    {
        public int Position { get => (int)(_ptr - _start); set => _ptr = _start + value; }
        public int Length => _length;
        private int _size;
        private int _length;
        private nint _start;
        private nint _ptr;
        private bool _disposedValue;

        public void MoveToStart()
        {
            _ptr = _start;
        }

        public byte ReadByte()
        {
            byte v = *(byte*)_ptr;
            _ptr += sizeof(byte);
            return v;
        }

        public sbyte ReadSByte()
        {
            sbyte v = *(sbyte*)_ptr;
            _ptr += sizeof(sbyte);
            return v;
        }

        public short ReadInt16()
        {
            short v = *(short*)_ptr;
            _ptr += sizeof(short);
            return v;
        }

        public ushort ReadUInt16()
        {
            ushort v = *(ushort*)_ptr;
            _ptr += sizeof(ushort);
            return v;
        }

        public int ReadInt32()
        {
            int v = *(int*)_ptr;
            _ptr += sizeof(int);
            return v;
        }

        public uint ReadUInt32()
        {
            uint v = *(uint*)_ptr;
            _ptr += sizeof(uint);
            return v;
        }

        public long ReadInt64()
        {
            long v = *(long*)_ptr;
            _ptr += sizeof(long);
            return v;
        }

        public ulong ReadUInt64()
        {
            ulong v = *(ulong*)_ptr;
            _ptr += sizeof(ulong);
            return v;
        }

        public Half ReadHalf()
        {
            Half v = *(Half*)_ptr;
            _ptr += sizeof(Half);
            return v;
        }

        public float ReadSingle()
        {
            float v = *(float*)_ptr;
            _ptr += sizeof(float);
            return v;
        }

        public double ReadDouble()
        {
            double v = *(double*)_ptr;
            _ptr += sizeof(double);
            return v;
        }

        public string ReadString()
        {
            Encoding encoding = Encoding.Default;

            int length = *(int*)_ptr;
            _ptr += sizeof(int);

            string v = encoding.GetString((byte*)_ptr, length);
            _ptr += length;
            return v;
        }

        public void SkipBytes(int count)
        {
            _ptr += count;
        }

        public void SetBytes(byte[] bytes)
        {
            EnsureCapacity(bytes.Length);
            _ptr = _start;
            _length = bytes.Length;
            Marshal.Copy(bytes, 0, _start, bytes.Length);
        }

        private void EnsureCapacity(int bytes)
        {
            if (_size < bytes)
            {
                if (_start != 0)
                {
                    Marshal.FreeHGlobal(_start);
                }
                _size = bytes;
                _start = Marshal.AllocHGlobal(_size);
                _ptr = _start;
            }
        }

        public UnsafeByteReader()
        {
            _size = 0;
            _start = 0;
            _ptr = 0;
        }

        public UnsafeByteReader(byte[] bytes)
        {
            _size = bytes.Length;
            _length = bytes.Length;
            _start = Marshal.AllocHGlobal(bytes.Length);
            _ptr = _start;
            Marshal.Copy(bytes, 0, _start, bytes.Length);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (_start != 0)
                {
                    Marshal.FreeHGlobal(_start);
                    _start = 0;
                    _ptr = 0;
                    _size = 0;
                }
                _disposedValue = true;
            }
        }

        ~UnsafeByteReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
