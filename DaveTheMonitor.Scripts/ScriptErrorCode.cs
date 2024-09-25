namespace DaveTheMonitor.Scripts
{
    public enum ScriptErrorCode
    {
        // 1x = Tokenizer Error
        // 2x = Parser Error
        // 3x = Semantic Error
        // 4x = Code Gen Error
        // 5x = Runtime Error

        #region Tokenizer

        T_InvalidIdentifier = 1001,
        T_UnexpectedEscape = 1002,
        T_UnexpectedChar = 1003,

        #endregion

        #region Parser

        P_EndOfScript = 2001,
        P_UnexpectedToken = 2002,
        P_ExpectedChar = 2003,
        P_ExpectedIdentifier = 2004,
        P_ExpectedAssignment = 2005,
        P_ExpectedExpr = 2006,
        P_ExpectedStatement = 2007,
        P_ExpectedKeyword = 2008,
        P_ExpectedBody = 2009,

        #endregion

        #region Semantic

        S_InvalidUsing = 3001,
        S_InvalidInVar = 3002,
        S_DuplicateInVar = 3003,
        S_InvalidType = 3004,
        S_AmbiguousMatch = 3005,
        S_ConstAssignment = 3006,
        S_NoVarDecl = 3007,
        S_InvalidOperand = 3008,
        S_InvalidCondition = 3009,
        S_InvalidVar = 3010,
        S_InvalidIteration = 3011,
        S_InvalidLoopCount = 3012,
        S_InvalidJump = 3013,
        S_UnsupportedExpr = 3014,
        S_UnsupportedStatement = 3015,
        S_OutOfRange = 3016,
        S_InvalidMember = 3017,
        S_InvalidArgCount = 3018,
        S_InvalidArgType = 3019,
        S_InvalidExpressionType = 3020,
        S_InvalidConstructor = 3021,
        S_InvalidDeclaration = 3022,

        #endregion

        #region CodeGen

        C_UnsupportedExpr = 4001,
        C_UnsupportedStatement = 4002,

        #endregion

        #region Runtime

        R_InvalidOperation = 5001,
        R_InvalidMemberInvoke = 5002,
        R_InvalidMember = 5003,
        R_NoPropertyGetter = 5004,
        R_NoPropertySetter = 5005,
        R_ArgCountError = 5006,
        R_ArgTypeError = 5007,
        R_InVarTypeError = 5008,
        R_InvalidStackAccess = 5009,
        R_InvalidOperand = 5010,
        R_StackOverflow = 5011,
        R_OutOfReferences = 5012,
        R_StackNotEmpty = 5013,
        R_ReferenceNotRemoved = 5014,
        R_MaxLocalsExceeded = 5015,
        R_MaxStackSizeExceeded = 5015,
        R_ReadonlyCollection = 5016,
        R_OutOfBounds = 5017,
        R_InvalidArg = 5018,

        #endregion
    }
}
