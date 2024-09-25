namespace DaveTheMonitor.Scripts.Runtime
{
    public enum ScriptOp
    {
        /// <summary>
        /// Performs no operation.
        /// </summary>
        Nop,
        /// <summary>
        /// Loads null onto the stack.
        /// </summary>
        NullLiteral,
        /// <summary>
        /// Loads the specified double onto the stack.
        /// </summary>
        /// <remarks>
        /// double value
        /// </remarks>
        DoubleLiteral,
        /// <summary>
        /// Loads 0f onto the stack.
        /// </summary>
        DoubleLiteral_0,
        /// <summary>
        /// Loads 1f onto the stack.
        /// </summary>
        DoubleLiteral_1,
        /// <summary>
        /// Loads the specified long onto the stack.
        /// </summary>
        /// <remarks>
        /// long value
        /// </remarks>
        LongLiteral,
        /// <summary>
        /// Loads 0 onto the stack.
        /// </summary>
        LongLiteral_0,
        /// <summary>
        /// Loads 1 onto the stack.
        /// </summary>
        LongLiteral_1,
        /// <summary>
        /// Loads the string at the specified index onto the stack.
        /// </summary>
        /// <remarks>
        /// uint16 index
        /// </remarks>
        StringLiteral,
        /// <summary>
        /// Loads the True onto the stack.
        /// </summary>
        TrueLiteral,
        /// <summary>
        /// Loads the False onto the stack.
        /// </summary>
        FalseLiteral,
        /// <summary>
        /// Pops and discards the value on top of the stack.
        /// </summary>
        Pop,
        /// <summary>
        /// Pops and discards the value on top of the stack without modifying references.
        /// </summary>
        Pop_NoRef,
        /// <summary>
        /// Duplicates the value on top of the stack.
        /// </summary>
        Dup,
        /// <summary>
        /// Duplicates the value on top of the stack without modifying references.
        /// </summary>
        Dup_NoRef,
        /// <summary>
        /// Performs type checking, adds the two values on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Add,
        /// <summary>
        /// Performs type checking, subtracts the two values on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Sub,
        /// <summary>
        /// Performs type checking, mulitplies the two values on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Mul,
        /// <summary>
        /// Performs type checking, divides the two values on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Div,
        /// <summary>
        /// Performs type checking, modulo the two values on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Mod,
        /// <summary>
        /// Adds the two values on top of the stack and pushes the result onto the stack. The first value must be a string, the second value can be any type.
        /// </summary>
        AddStr,
        /// <summary>
        /// Tests if the two values on top of the stack are equal and pushes the result onto the stack.
        /// </summary>
        Eq,
        /// <summary>
        /// Tests if the two values on top of the stack are not equal and pushes the result onto the stack.
        /// </summary>
        Neq,
        /// <summary>
        /// Performs type checking, compares the two values on top of the stack, and pushes True if the left < right.
        /// </summary>
        Lt,
        /// <summary>
        /// Performs type checking, compares the two values on top of the stack, and pushes True if the left <= right.
        /// </summary>
        Lte,
        /// <summary>
        /// Performs type checking, compares the two values on top of the stack, and pushes True if the left > right.
        /// </summary>
        Gt,
        /// <summary>
        /// Performs type checking, compares the two values on top of the stack, and pushes True if the left >= right.
        /// </summary>
        Gte,
        /// <summary>
        /// Performs type checking, inverts the value on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Invert,
        /// <summary>
        /// Performs type checking, negates the value on top of the stack, and pushes the result onto the stack.
        /// </summary>
        Neg,
        /// <summary>
        /// Pushes True onto the stack if both of the two values on top of the stack are True.
        /// </summary>
        And,
        /// <summary>
        /// Pushes True onto the stack if one of the two values on top of the stack is True.
        /// </summary>
        Or,
        /// <summary>
        /// Prints the value on top of the stack.
        /// </summary>
        Print,
        /// <summary>
        /// Immediately exits the script and returns Null.
        /// </summary>
        Exit,
        /// <summary>
        /// Sets the specified local to the value on top of the stack.
        /// </summary>
        /// <remarks>
        /// byte local
        /// </remarks>
        SetLoc,
        /// <summary>
        /// Sets local 0 to the value on top of the stack.
        /// </summary>
        SetLoc_0,
        /// <summary>
        /// Sets local 1 to the value on top of the stack.
        /// </summary>
        SetLoc_1,
        /// <summary>
        /// Sets local 2 to the value on top of the stack.
        /// </summary>
        SetLoc_2,
        /// <summary>
        /// Sets local 3 to the value on top of the stack.
        /// </summary>
        SetLoc_3,
        /// <summary>
        /// Loads the specified local and pushes it onto the stack.
        /// </summary>
        /// <remarks>
        /// byte local
        /// </remarks>
        LoadLoc,
        /// <summary>
        /// Loads local 0 and pushes it onto the stack.
        /// </summary>
        LoadLoc_0,
        /// <summary>
        /// Loads local 1 and pushes it onto the stack.
        /// </summary>
        LoadLoc_1,
        /// <summary>
        /// Loads local 2 and pushes it onto the stack.
        /// </summary>
        LoadLoc_2,
        /// <summary>
        /// Loads local 3 and pushes it onto the stack.
        /// </summary>
        LoadLoc_3,
        /// <summary>
        /// Unconditionally jumps to the specified position of the bytecode.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        Jump,
        /// <summary>
        /// Jumps to the specified position of the bytecode if the value on top of the stack is True.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpT,
        /// <summary>
        /// Jumps to the specified position of the bytecode if the value on top of the stack is False.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpF,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if they are equal.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpEq,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if they are not equal.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpNeq,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if left < right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpLt,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if left <= right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpLte,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if left > right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpGt,
        /// <summary>
        /// Compares the two values on top of the stack and jumps to the specified position of the bytecode if left >= right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpGte,
        /// <summary>
        /// Invokes the method with the specified ID without performing type checking and pushes the result onto the stack.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        Invoke,
        /// <summary>
        /// Invokes the method with the specified name, performs type checking, and pushes the result onto the stack.
        /// Also functions as a dynamic property getter.
        /// </summary>
        /// <remarks>
        /// uint16 nameIndex,
        /// uint16 args
        /// </remarks>
        InvokeDynamic,
        /// <summary>
        /// Invokes the static method with the specified ID without performing type checking and pushes the result onto the stack.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        InvokeStatic,
        /// <summary>
        /// Gets the value of the property with the specified ID without performing type checking and pushes the result onto the stack.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        GetProperty,
        /// <summary>
        /// Sets the property with the specified ID to the value on top of the stack without performing type checking.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        SetProperty,
        /// <summary>
        /// Performs type checking and sets the property with the specified name to the value on top of the stack.
        /// </summary>
        /// <remarks>
        /// uint16 nameIndex
        /// </remarks>
        SetDynamicProperty,
        /// <summary>
        /// Gets the value of the static property with the specified ID without performing type checking and pushes the result onto the stack.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        GetStaticProperty,
        /// <summary>
        /// Sets the static property with the specified ID to the value on top of the stack without performing type checking.
        /// </summary>
        /// <remarks>
        /// int32 id
        /// </remarks>
        SetStaticProperty,
        /// <summary>
        /// Throws a runtime error if the value on top of the stack is not the specified type.
        /// </summary>
        /// <remarks>
        /// int32 typeId
        /// </remarks>
        CheckType,
        /// <summary>
        /// Throws a runtime error if the value on top of the stack is not the specified type, without popping the value.
        /// </summary>
        /// <remarks>
        /// int32 typeId
        /// </remarks>
        PeekCheckType,
        /// <summary>
        /// Loops the specified bytecode the specified number of times.
        /// </summary>
        /// <remarks>
        /// int32 count
        /// int64 length
        /// </remarks>
        Loop,
        /// <summary>
        /// Loops the specified bytecode x times, where x is the value on top of the stack.
        /// </summary>
        /// <remarks>
        /// int32 length
        /// </remarks>
        Loop_Num,
        /// <summary>
        /// Adds the two numeric values on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Add_Num,
        /// <summary>
        /// Subtracts the two numeric values on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Sub_Num,
        /// <summary>
        /// Multiplies the two numeric values on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Mul_Num,
        /// <summary>
        /// Divides the two numeric values on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Div_Num,
        /// <summary>
        /// Modulo the two numeric values on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Mod_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking, and pushes True if the left < right.
        /// </summary>
        Lt_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking, and pushes True if the left <= right.
        /// </summary>
        Lte_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking, and pushes True if the left > right.
        /// </summary>
        Gt_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking, and pushes True if the left >= right.
        /// </summary>
        Gte_Num,
        /// <summary>
        /// Negates the numeric value on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Neg_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if they are equal.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpEq_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if they are not equal.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpNeq_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if left < right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpLt_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if left <= right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpLte_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if left > right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpGt_Num,
        /// <summary>
        /// Compares the two numeric values on top of the stack without type checking and jumps to the specified position of the bytecode if left >= right.
        /// </summary>
        /// <remarks>
        /// int32 position
        /// </remarks>
        JumpGte_Num,
        /// <summary>
        /// Sets the specified local to the value on top of the stack without modifying references.
        /// </summary>
        /// <remarks>
        /// byte local
        /// </remarks>
        SetLoc_NoRef,
        /// <summary>
        /// Sets the local 0 to the value on top of the stack without modifying references.
        /// </summary>
        SetLoc_0_NoRef,
        /// <summary>
        /// Sets the local 1 to the value on top of the stack without modifying references.
        /// </summary>
        SetLoc_1_NoRef,
        /// <summary>
        /// Sets the local 2 to the value on top of the stack without modifying references.
        /// </summary>
        SetLoc_2_NoRef,
        /// <summary>
        /// Sets the local 3 to the value on top of the stack without modifying references.
        /// </summary>
        SetLoc_3_NoRef,
        /// <summary>
        /// Loads the specified local and pushes it onto the stack without modifying references.
        /// </summary>
        /// <remarks>
        /// byte local
        /// </remarks>
        LoadLoc_NoRef,
        /// <summary>
        /// Loads the local 0 and pushes it onto the stack without modifying references.
        /// </summary>
        LoadLoc_0_NoRef,
        /// <summary>
        /// Loads the local 1 and pushes it onto the stack without modifying references.
        /// </summary>
        LoadLoc_1_NoRef,
        /// <summary>
        /// Loads the local 2 and pushes it onto the stack without modifying references.
        /// </summary>
        LoadLoc_2_NoRef,
        /// <summary>
        /// Loads the local 3 and pushes it onto the stack without modifying references.
        /// </summary>
        LoadLoc_3_NoRef,
        /// <summary>
        /// Inverts the boolean value on top of the stack without type checking and pushes the result onto the stack.
        /// </summary>
        Invert_Bool,
        /// <summary>
        /// Returns the value on top of the stack if there is one, otherwise returns Null.
        /// </summary>
        Return
    }
}
