namespace FalseDotNet.Operations;

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
    WhileInit, WhileCondition, WhileBody,
    Exit,

// Names

    Ref,
    Store,
    Load,

// I/O

    OutputChar,
    OutputDecimal,
}