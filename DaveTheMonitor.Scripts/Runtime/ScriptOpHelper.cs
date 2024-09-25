namespace DaveTheMonitor.Scripts.Runtime
{
    public static class ScriptOpHelper
    {
        public static int GetBytes(ScriptOp op)
        {
            return op switch
            {
                ScriptOp.SetLoc or
                ScriptOp.LoadLoc or
                ScriptOp.SetLoc_NoRef or
                ScriptOp.LoadLoc_NoRef => sizeof(byte),

                ScriptOp.StringLiteral or
                ScriptOp.SetDynamicProperty => sizeof(ushort),

                ScriptOp.InvokeDynamic => sizeof(ushort) * 2,

                ScriptOp.LongLiteral => sizeof(long),

                ScriptOp.Jump or
                ScriptOp.JumpT or
                ScriptOp.JumpF or
                ScriptOp.JumpEq or
                ScriptOp.JumpNeq or
                ScriptOp.JumpLt or
                ScriptOp.JumpLte or
                ScriptOp.JumpGt or
                ScriptOp.JumpGte or
                ScriptOp.JumpEq_Num or
                ScriptOp.JumpNeq_Num or
                ScriptOp.JumpLt_Num or
                ScriptOp.JumpLte_Num or
                ScriptOp.JumpGt_Num or
                ScriptOp.JumpGte_Num or
                ScriptOp.Invoke or
                ScriptOp.InvokeStatic or
                ScriptOp.GetProperty or
                ScriptOp.SetProperty or
                ScriptOp.GetStaticProperty or
                ScriptOp.SetStaticProperty or
                ScriptOp.CheckType or
                ScriptOp.PeekCheckType or
                ScriptOp.Loop_Num => sizeof(int),

                ScriptOp.Loop => sizeof(int) + sizeof(long),

                ScriptOp.DoubleLiteral => sizeof(double),

                _ => 0
            };
        }

        public static bool IsJump(ScriptOp op)
        {
            return op == ScriptOp.Jump ||
                op == ScriptOp.JumpT ||
                op == ScriptOp.JumpF ||
                op == ScriptOp.JumpEq ||
                op == ScriptOp.JumpNeq ||
                op == ScriptOp.JumpLt ||
                op == ScriptOp.JumpLte ||
                op == ScriptOp.JumpGt ||
                op == ScriptOp.JumpGte ||
                op == ScriptOp.JumpEq_Num ||
                op == ScriptOp.JumpNeq_Num ||
                op == ScriptOp.JumpLt_Num ||
                op == ScriptOp.JumpLte_Num ||
                op == ScriptOp.JumpGt_Num ||
                op == ScriptOp.JumpGte_Num;
        }

        public static bool IsSetLoc(ScriptOp op)
        {
            return op == ScriptOp.SetLoc ||
                op == ScriptOp.SetLoc_0 ||
                op == ScriptOp.SetLoc_1 ||
                op == ScriptOp.SetLoc_2 ||
                op == ScriptOp.SetLoc_3 ||
                op == ScriptOp.SetLoc_NoRef ||
                op == ScriptOp.SetLoc_0_NoRef ||
                op == ScriptOp.SetLoc_1_NoRef ||
                op == ScriptOp.SetLoc_2_NoRef ||
                op == ScriptOp.SetLoc_3_NoRef;
        }

        public static bool IsLoadLoc(ScriptOp op)
        {
            return op == ScriptOp.LoadLoc ||
                op == ScriptOp.LoadLoc_0 ||
                op == ScriptOp.LoadLoc_1 ||
                op == ScriptOp.LoadLoc_2 ||
                op == ScriptOp.LoadLoc_3 ||
                op == ScriptOp.LoadLoc_NoRef ||
                op == ScriptOp.LoadLoc_0_NoRef ||
                op == ScriptOp.LoadLoc_1_NoRef ||
                op == ScriptOp.LoadLoc_2_NoRef ||
                op == ScriptOp.LoadLoc_3_NoRef;
        }
    }
}
