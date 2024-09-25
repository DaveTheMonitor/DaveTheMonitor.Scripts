using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace DaveTheMonitor.Scripts.Utilities
{
    public sealed class ByteReader
    {
        public int Position { get => _offset; set => _offset = value; }
        public int Length => _bytes.Length;
        private byte[] _bytes;
        private int _offset;

        public byte ReadByte()
        {
            return _bytes[_offset++];
        }

        public sbyte ReadSByte()
        {
            return Unsafe.As<byte, sbyte>(ref _bytes[_offset++]);
        }

        public short ReadInt16()
        {
            short v = Unsafe.ReadUnaligned<short>(ref _bytes[_offset]);
            _offset += sizeof(short);
            return v;
        }

        public ushort ReadUInt16()
        {
            ushort v = Unsafe.ReadUnaligned<ushort>(ref _bytes[_offset]);
            _offset += sizeof(ushort);
            return v;
        }

        public int ReadInt32()
        {
            int v = Unsafe.ReadUnaligned<int>(ref _bytes[_offset]);
            _offset += sizeof(int);
            return v;
        }

        public uint ReadUInt32()
        {
            uint v = Unsafe.ReadUnaligned<uint>(ref _bytes[_offset]);
            _offset += sizeof(uint);
            return v;
        }

        public long ReadInt64()
        {
            long v = Unsafe.ReadUnaligned<long>(ref _bytes[_offset]);
            _offset += sizeof(long);
            return v;
        }

        public ulong ReadUInt64()
        {
            ulong v = Unsafe.ReadUnaligned<ulong>(ref _bytes[_offset]);
            _offset += sizeof(ulong);
            return v;
        }

        public Half ReadHalf()
        {
            Half v = Unsafe.ReadUnaligned<Half>(ref _bytes[_offset]);
            _offset += 2;
            return v;
        }

        public float ReadSingle()
        {
            float v = Unsafe.ReadUnaligned<float>(ref _bytes[_offset]);
            _offset += sizeof(float);
            return v;
        }

        public double ReadDouble()
        {
            double v = Unsafe.ReadUnaligned<double>(ref _bytes[_offset]);
            _offset += sizeof(double);
            return v;
        }

        public decimal ReadDecimal()
        {
            decimal v = Unsafe.ReadUnaligned<decimal>(ref _bytes[_offset]);
            _offset += sizeof(decimal);
            return v;
        }

        public string ReadString()
        {
            int length = Unsafe.ReadUnaligned<int>(ref _bytes[_offset]);
            string v = Encoding.Default.GetString(_bytes.AsSpan(_offset + sizeof(int), length));
            _offset += sizeof(int) + length;
            return v;
        }

        public T Read<T>() where T : struct
        {
            T v = Unsafe.ReadUnaligned<T>(ref _bytes[_offset]);
            _offset += Unsafe.SizeOf<T>();
            return v;
        }

        public byte[] ReadBytes(int byteCount)
        {
            byte[] arr = new byte[byteCount];
            ReadBytes(arr, byteCount);
            return arr;
        }

        public void ReadBytes(byte[] bytes, int byteCount)
        {
            Unsafe.CopyBlockUnaligned(ref bytes[0], ref _bytes[_offset], (uint)byteCount);
            _offset += byteCount;
        }

        public void ReadBytes(Span<byte> bytes, int byteCount)
        {
            Unsafe.CopyBlockUnaligned(ref bytes[0], ref _bytes[_offset], (uint)byteCount);
            _offset += byteCount;
        }

        public void SetBytes(byte[] bytes)
        {
            _bytes = bytes;
            _offset = 0;
        }

        public ByteReader()
        {
            _bytes = null;
            _offset = 0;
        }

        public ByteReader(byte[] bytes)
        {
            _bytes = bytes;
            _offset = 0;
        }
    }
}
