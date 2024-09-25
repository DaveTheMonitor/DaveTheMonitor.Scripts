using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DaveTheMonitor.Scripts.Utilities
{
    public sealed class ByteWriter
    {
        public int Position { get => _offset; set => _offset = value; }
        private byte[] _bytes;
        private int _offset;
        private int _length;

        public void Write(byte value)
        {
            Expand(sizeof(byte));
            _bytes[_offset++] = value;
            SetLength(_offset);
        }

        public void Write(sbyte value)
        {
            Expand(sizeof(sbyte));
            _bytes[_offset++] = Unsafe.As<sbyte, byte>(ref value);
            SetLength(_offset);
        }

        public void Write(short value)
        {
            Expand(sizeof(short));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(short);
            SetLength(_offset);
        }

        public void Write(ushort value)
        {
            Expand(sizeof(ushort));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(ushort);
            SetLength(_offset);
        }

        public void Write(int value)
        {
            Expand(sizeof(int));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(int);
            SetLength(_offset);
        }

        public void Write(uint value)
        {
            Expand(sizeof(uint));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(uint);
            SetLength(_offset);
        }

        public void Write(long value)
        {
            Expand(sizeof(long));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(long);
            SetLength(_offset);
        }

        public void Write(ulong value)
        {
            Expand(sizeof(ulong));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(ulong);
            SetLength(_offset);
        }

        public void Write(Half value)
        {
            Expand(2);
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += 2;
            SetLength(_offset);
        }

        public void Write(float value)
        {
            Expand(sizeof(float));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(float);
            SetLength(_offset);
        }

        public void Write(double value)
        {
            Expand(sizeof(double));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(double);
            SetLength(_offset);
        }

        public void Write(decimal value)
        {
            Expand(sizeof(decimal));
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += sizeof(decimal);
            SetLength(_offset);
        }

        public void Write(string value)
        {
            Encoding encoding = Encoding.Default;
            int length = encoding.GetByteCount(value);
            Expand(sizeof(int) + length);
            Unsafe.WriteUnaligned(ref _bytes[_offset], length);

            encoding.GetBytes(value, _bytes.AsSpan(_offset + sizeof(int), length));
            _offset += sizeof(int) + length;
            SetLength(_offset);
        }

        public void Write<T>(T value) where T : struct
        {
            Expand(Unsafe.SizeOf<T>());
            Unsafe.WriteUnaligned(ref _bytes[_offset], value);
            _offset += Unsafe.SizeOf<T>();
            SetLength(_offset);
        }

        public void Write(byte[] bytes, int byteCount)
        {
            Write(new ReadOnlySpan<byte>(bytes), byteCount);
        }

        public void Write(ReadOnlySpan<byte> bytes, int byteCount)
        {
            Expand(bytes.Length);
            Unsafe.CopyBlockUnaligned(ref _bytes[_offset], ref MemoryMarshal.GetReference(bytes), (uint)byteCount);
            _offset += byteCount;
        }

        private void Expand(int length)
        {
            int target = _offset + length;
            if (_length >= target)
            {
                return;
            }

            int newLength = Math.Max(_length * 2, _length + length);
            Array.Resize(ref _bytes, newLength);
        }

        private void SetLength(int offset)
        {
            _length = Math.Max(offset, _length);
        }

        public void SetBytes(byte[] bytes)
        {
            _bytes = bytes;
            _offset = 0;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[_length];
            Array.Copy(_bytes, bytes, _length);
            return bytes;
        }

        public ByteWriter()
        {
            _bytes = null;
            _offset = 0;
        }

        public ByteWriter(int length)
        {
            _bytes = new byte[length];
            _offset = 0;
        }

        public ByteWriter(byte[] bytes)
        {
            _bytes = new byte[bytes.Length];
            Array.Copy(bytes, _bytes, bytes.Length);
            _offset = 0;
        }
    }
}
