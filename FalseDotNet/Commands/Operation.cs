namespace FalseDotNet.Commands;

public enum Operation
{
    IntLiteral,

// Stack

    Dup,
    Drop,
    Swap,
    Rot,
    Pick,

// Arithmetic

    Add,
    Sub,
    Mul,
    Div,
    Neg,
    And,
    Or,
    Not,

// Comparison

    Gt,
    Eq,

// Lambda and Flow control

    Lambda,
    Ret,
    Execute,
    ConditionalExecute,
    WhileInit,
    WhileCondition,
    WhileBody,
    Exit,

// Names

    Ref,
    Store,
    Load,

// I/O

    PrintString,
    OutputChar,
    OutputDecimal,
    Flush,
}

public static class OperationExtensions
{
    public static bool HasArgument(this Operation op) => op switch
    {
        Operation.IntLiteral => true,
        Operation.Lambda => true,
        Operation.Ref => true,
        Operation.PrintString => true,
        _ => false
    };
}