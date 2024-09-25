using DaveTheMonitor.Scripts.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaveTheMonitor.Scripts
{
    public unsafe sealed partial class ScriptRuntime : IScriptRuntime
    {
        private struct RuntimeMethod
        {
            public delegate*<ScriptRuntime, void> M;
            public RuntimeMethod(delegate*<ScriptRuntime, void> m)
            {
                M = m;
            }
        }

        private static RuntimeMethod* _ops;

        static void InitOps()
        {
            _ops = (RuntimeMethod*)Marshal.AllocHGlobal(sizeof(RuntimeMethod) * ((int)ScriptOp.Return + 1));
            int i = 0;
            _ops[i++] = new RuntimeMethod(&Nop);
            _ops[i++] = new RuntimeMethod(&NullLiteral);
            _ops[i++] = new RuntimeMethod(&DoubleLiteral);
            _ops[i++] = new RuntimeMethod(&DoubleLiteral_0);
            _ops[i++] = new RuntimeMethod(&DoubleLiteral_1);
            _ops[i++] = new RuntimeMethod(&LongLiteral);
            _ops[i++] = new RuntimeMethod(&LongLiteral_0);
            _ops[i++] = new RuntimeMethod(&LongLiteral_1);
            _ops[i++] = new RuntimeMethod(&StringLiteral);
            _ops[i++] = new RuntimeMethod(&TrueLiteral);
            _ops[i++] = new RuntimeMethod(&FalseLiteral);
            _ops[i++] = new RuntimeMethod(&Pop);
            _ops[i++] = new RuntimeMethod(&Pop_NoRef);
            _ops[i++] = new RuntimeMethod(&Dup);
            _ops[i++] = new RuntimeMethod(&Dup_NoRef);
            _ops[i++] = new RuntimeMethod(&Add);
            _ops[i++] = new RuntimeMethod(&Sub);
            _ops[i++] = new RuntimeMethod(&Mul);
            _ops[i++] = new RuntimeMethod(&Div);
            _ops[i++] = new RuntimeMethod(&Mod);
            _ops[i++] = new RuntimeMethod(&AddStr);
            _ops[i++] = new RuntimeMethod(&Eq);
            _ops[i++] = new RuntimeMethod(&Neq);
            _ops[i++] = new RuntimeMethod(&Lt);
            _ops[i++] = new RuntimeMethod(&Lte);
            _ops[i++] = new RuntimeMethod(&Gt);
            _ops[i++] = new RuntimeMethod(&Gte);
            _ops[i++] = new RuntimeMethod(&Invert);
            _ops[i++] = new RuntimeMethod(&Neg);
            _ops[i++] = new RuntimeMethod(&And);
            _ops[i++] = new RuntimeMethod(&Or);
            _ops[i++] = new RuntimeMethod(&Print);
            _ops[i++] = new RuntimeMethod(&Exit);
            _ops[i++] = new RuntimeMethod(&SetLoc);
            _ops[i++] = new RuntimeMethod(&SetLoc_0);
            _ops[i++] = new RuntimeMethod(&SetLoc_1);
            _ops[i++] = new RuntimeMethod(&SetLoc_2);
            _ops[i++] = new RuntimeMethod(&SetLoc_3);
            _ops[i++] = new RuntimeMethod(&LoadLoc);
            _ops[i++] = new RuntimeMethod(&LoadLoc_0);
            _ops[i++] = new RuntimeMethod(&LoadLoc_1);
            _ops[i++] = new RuntimeMethod(&LoadLoc_2);
            _ops[i++] = new RuntimeMethod(&LoadLoc_3);
            _ops[i++] = new RuntimeMethod(&Jump);
            _ops[i++] = new RuntimeMethod(&JumpT);
            _ops[i++] = new RuntimeMethod(&JumpF);
            _ops[i++] = new RuntimeMethod(&JumpEq);
            _ops[i++] = new RuntimeMethod(&JumpNeq);
            _ops[i++] = new RuntimeMethod(&JumpLt);
            _ops[i++] = new RuntimeMethod(&JumpLte);
            _ops[i++] = new RuntimeMethod(&JumpGt);
            _ops[i++] = new RuntimeMethod(&JumpGte);
            _ops[i++] = new RuntimeMethod(&Invoke);
            _ops[i++] = new RuntimeMethod(&InvokeDynamic);
            _ops[i++] = new RuntimeMethod(&InvokeStatic);
            _ops[i++] = new RuntimeMethod(&GetProperty);
            _ops[i++] = new RuntimeMethod(&SetProperty);
            _ops[i++] = new RuntimeMethod(&SetDynamicProperty);
            _ops[i++] = new RuntimeMethod(&GetStaticProperty);
            _ops[i++] = new RuntimeMethod(&SetStaticProperty);
            _ops[i++] = new RuntimeMethod(&CheckType);
            _ops[i++] = new RuntimeMethod(&PeekCheckType);
            _ops[i++] = new RuntimeMethod(&Loop);
            _ops[i++] = new RuntimeMethod(&Loop_Num);
            _ops[i++] = new RuntimeMethod(&Add_Num);
            _ops[i++] = new RuntimeMethod(&Sub_Num);
            _ops[i++] = new RuntimeMethod(&Mul_Num);
            _ops[i++] = new RuntimeMethod(&Div_Num);
            _ops[i++] = new RuntimeMethod(&Mod_Num);
            _ops[i++] = new RuntimeMethod(&Lt_Num);
            _ops[i++] = new RuntimeMethod(&Lte_Num);
            _ops[i++] = new RuntimeMethod(&Gt_Num);
            _ops[i++] = new RuntimeMethod(&Gte_Num);
            _ops[i++] = new RuntimeMethod(&Neg_Num);
            _ops[i++] = new RuntimeMethod(&JumpEq_Num);
            _ops[i++] = new RuntimeMethod(&JumpNeq_Num);
            _ops[i++] = new RuntimeMethod(&JumpLt_Num);
            _ops[i++] = new RuntimeMethod(&JumpLte_Num);
            _ops[i++] = new RuntimeMethod(&JumpGt_Num);
            _ops[i++] = new RuntimeMethod(&JumpGte_Num);
            _ops[i++] = new RuntimeMethod(&SetLoc_NoRef);
            _ops[i++] = new RuntimeMethod(&SetLoc_0_NoRef);
            _ops[i++] = new RuntimeMethod(&SetLoc_1_NoRef);
            _ops[i++] = new RuntimeMethod(&SetLoc_2_NoRef);
            _ops[i++] = new RuntimeMethod(&SetLoc_3_NoRef);
            _ops[i++] = new RuntimeMethod(&LoadLoc_NoRef);
            _ops[i++] = new RuntimeMethod(&LoadLoc_0_NoRef);
            _ops[i++] = new RuntimeMethod(&LoadLoc_1_NoRef);
            _ops[i++] = new RuntimeMethod(&LoadLoc_2_NoRef);
            _ops[i++] = new RuntimeMethod(&LoadLoc_3_NoRef);
            _ops[i++] = new RuntimeMethod(&Invert_Bool);
            _ops[i++] = new RuntimeMethod(&Return);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallOp(ScriptOp op)
        {
            // this is faster than a switch statement
            // (on my machine at least, should probably be tested on other machines as well)
            _ops[(int)op].M(this);
        }

        private void DisposeOps()
        {
            Marshal.FreeHGlobal((nint)_ops);
        }
    }
}
