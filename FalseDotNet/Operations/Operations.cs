namespace FalseDotNet.Operations;

public static class Op
{
    // Literals

    public static IntLiteralOp Int(long value) => new IntLiteralOp { Value = value };

    // Stack

    public static readonly DupOp Dup = new();
    public static readonly DropOp Drop = new();
    public static readonly SwapOp Swap = new();
    public static readonly RotOp Rot = new();
    public static readonly PickOp Pick = new();

    // Arithmetic

    public static readonly AddOp Add = new();
    public static readonly SubOp Sub = new();
    public static readonly MulOp Mul = new();
    public static readonly DivOp Div = new();
    public static readonly NegOp Neg = new();
    public static readonly AndOp And = new();
    public static readonly OrOp Or = new();
    public static readonly NotOp Not = new();

    // Comparison

    public static readonly GtOp Gt = new();
    public static readonly EqOp Eq = new();

    // Names

    public static RefOp Ref(long index) => new RefOp { Index = index };
    public static readonly StoreOp Store = new();
    public static readonly LoadOp Load = new();
    
    // I/O

    public static readonly OutputCharOp OutputChar = new();
    public static readonly OutputDecimalOp OutputDecimal = new();
}

// Literals

public class IntLiteralOp : IOperation
{
    public long Value { get; init; }

    public override string ToString()
        => $"INT({Value})";
}

// Stack

public class DupOp : IOperation
{
    public override string ToString()
        => "DUP";
}

public class DropOp : IOperation
{
    public override string ToString()
        => "DROP";
}

public class SwapOp : IOperation
{
    public override string ToString()
        => "SWAP";
}

public class RotOp : IOperation
{
    public override string ToString()
        => "ROT";
}

public class PickOp : IOperation
{
    public override string ToString()
        => "PICK";
}

// Arithmetic

public class AddOp : IOperation
{
    public override string ToString()
        => "ADD";
}

public class SubOp : IOperation
{
    public override string ToString()
        => "SUB";
}

public class MulOp : IOperation
{
    public override string ToString()
        => "MUL";
}

public class DivOp : IOperation
{
    public override string ToString()
        => "DIV";
}

public class NegOp : IOperation
{
    public override string ToString()
        => "NEG";
}

public class AndOp : IOperation
{
    public override string ToString()
        => "AND";
}

public class OrOp : IOperation
{
    public override string ToString()
        => "OR";
}

public class NotOp : IOperation
{
    public override string ToString()
        => "NOT";
}

// Comparison

public class GtOp : IOperation
{
    public override string ToString()
        => "GT";
}

public class EqOp : IOperation
{
    public override string ToString()
        => "EQ";
}

// Names

public class RefOp : IOperation
{
    public long Index { get; init; }

    public override string ToString()
        => $"Ref({Index})";
}

public class StoreOp : IOperation
{
    public override string ToString()
        => "STORE";
}

public class LoadOp : IOperation
{
    public override string ToString()
        => "LOAD";
}

// I/O

public class OutputCharOp : IOperation
{
    public override string ToString()
        => "OUT_CHAR";
}

public class OutputDecimalOp : IOperation
{
    public override string ToString()
        => "OUT_DEC";
}