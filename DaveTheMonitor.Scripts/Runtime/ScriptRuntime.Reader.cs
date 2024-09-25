using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DaveTheMonitor.Scripts
{
    public unsafe sealed partial class ScriptRuntime : IScriptRuntime
    {
        private int ProgramPosition { get => (int)(_programPtr - _programStart); set => _programPtr = _programStart + value; }
        private int _programSize;
        private nint _programStart;
        private nint _programPtr;

        public void MoveToStart()
        {
            _programPtr = _programStart;
        }

        public byte ReadByte()
        {
            byte v = *(byte*)_programPtr;
            _programPtr += sizeof(byte);
            return v;
        }

        public sbyte ReadSByte()
        {
            sbyte v = *(sbyte*)_programPtr;
            _programPtr += sizeof(sbyte);
            return v;
        }

        public short ReadInt16()
        {
            short v = *(short*)_programPtr;
            _programPtr += sizeof(short);
            return v;
        }

        public ushort ReadUInt16()
        {
            ushort v = *(ushort*)_programPtr;
            _programPtr += sizeof(ushort);
            return v;
        }

        public int ReadInt32()
        {
            int v = *(int*)_programPtr;
            _programPtr += sizeof(int);
            return v;
        }

        public uint ReadUInt32()
        {
            uint v = *(uint*)_programPtr;
            _programPtr += sizeof(uint);
            return v;
        }

        public long ReadInt64()
        {
            long v = *(long*)_programPtr;
            _programPtr += sizeof(long);
            return v;
        }

        public ulong ReadUInt64()
        {
            ulong v = *(ulong*)_programPtr;
            _programPtr += sizeof(ulong);
            return v;
        }

        public Half ReadHalf()
        {
            Half v = *(Half*)_programPtr;
            _programPtr += sizeof(Half);
            return v;
        }

        public float ReadSingle()
        {
            float v = *(float*)_programPtr;
            _programPtr += sizeof(float);
            return v;
        }

        public double ReadDouble()
        {
            double v = *(double*)_programPtr;
            _programPtr += sizeof(double);
            return v;
        }

        public string ReadString()
        {
            Encoding encoding = Encoding.Default;

            int length = *(int*)_programPtr;
            _programPtr += sizeof(int);

            string v = encoding.GetString((byte*)_programPtr, length);
            _programPtr += length;
            return v;
        }

        public void SetBytes(byte[] bytes)
        {
            EnsureCapacity(bytes.Length);
            _programPtr = _programStart;
            Marshal.Copy(bytes, 0, _programStart, bytes.Length);
        }

        private void EnsureCapacity(int bytes)
        {
            if (_programSize < bytes)
            {
                if (_programStart != 0)
                {
                    Marshal.FreeHGlobal(_programStart);
                }
                _programSize = bytes;
                _programStart = Marshal.AllocHGlobal(_programSize);
                _programPtr = _programStart;
            }
        }

        private void DisposeProgram()
        {
            Marshal.FreeHGlobal(_programStart);
            _programStart = 0;
            _programPtr = 0;
            _programSize = 0;
        }
    }
}
