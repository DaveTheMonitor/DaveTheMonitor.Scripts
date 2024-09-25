using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaveTheMonitor.Scripts
{
    public unsafe sealed partial class ScriptRuntime : IScriptRuntime
    {
        public int StackSize { get; private set; }
        public int StackCount => (int)((_stackPtr - _stackStart) / sizeof(ScriptVar));
        private nint _stackStart;
        private nint _stackEnd;
        private nint _stackPtr;

        public void InitStack(int size)
        {
            StackSize = size;
            _stackStart = Marshal.AllocHGlobal(sizeof(ScriptVar) * size);
            _stackEnd = _stackStart + (sizeof(ScriptVar) * size);
            _stackPtr = _stackStart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(ScriptVar v)
        {
            if (_stackPtr < _stackEnd)
            {
                *(ScriptVar*)_stackPtr = v;
                _stackPtr += sizeof(ScriptVar);
            }
            else
            {
                Error(ScriptErrorCode.R_StackOverflow, "StackOverflow", "Stack overflow");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ScriptVar Pop()
        {
            if (_stackPtr > _stackStart)
            {
                _stackPtr -= sizeof(ScriptVar);
                return *(ScriptVar*)_stackPtr;
            }

            Error(ScriptErrorCode.R_InvalidStackAccess, "Invalid Stack Pop", "Invalid stack pop");
            return ScriptVar.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopLeftRight(out ScriptVar left, out ScriptVar right)
        {
            // By testing against _stackStart + sizeof(ScriptVar)
            // instead of _stackStart, we only need to do one
            // bounds check for two pops.
            if (_stackPtr > (_stackStart + sizeof(ScriptVar)))
            {
                _stackPtr -= sizeof(ScriptVar);
                right = *(ScriptVar*)_stackPtr;
                _stackPtr -= sizeof(ScriptVar);
                left = *(ScriptVar*)_stackPtr;
                return;
            }

            Error(ScriptErrorCode.R_InvalidStackAccess, "Invalid Stack Pop", "Invalid stack pop");
            left = ScriptVar.Null;
            right = ScriptVar.Null;
        }

        ScriptVar _popLeftResult;
        ScriptVar _popRightResult;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopLeftRight2()
        {
            // By testing against _stackStart + sizeof(ScriptVar)
            // instead of _stackStart, we only need to do one
            // bounds check for two pops.
            if (_stackPtr > (_stackStart + sizeof(ScriptVar)))
            {
                _stackPtr -= sizeof(ScriptVar);
                _popRightResult = *(ScriptVar*)_stackPtr;
                _stackPtr -= sizeof(ScriptVar);
                _popLeftResult = *(ScriptVar*)_stackPtr;
                return;
            }

            Error(ScriptErrorCode.R_InvalidStackAccess, "Invalid Stack Pop", "Invalid stack pop");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ScriptVar Peek()
        {
            if (_stackPtr > _stackStart)
            {
                return *(ScriptVar*)(_stackPtr - sizeof(ScriptVar));
            }

            Error(ScriptErrorCode.R_InvalidStackAccess, "Invalid Stack Peek", "Invalid stack peek");
            return ScriptVar.Null;
        }

        public void ClearStack()
        {
            _stackPtr = _stackStart;
        }

        private void DisposeStack()
        {
            Marshal.FreeHGlobal(_stackStart);
        }
    }
}
