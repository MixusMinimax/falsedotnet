namespace FalseDotNet.Operations;

public static class Op
{
    public static IntLiteralOp Int(ulong value) => new IntLiteralOp { Value = value };
    public static readonly AddOp Add = new();
    public static readonly OutputCharOp OutputChar = new();
    public static readonly OutputDecimalOp OutputDecimal = new();
}

public class IntLiteralOp : IOperation
{
    public ulong Value { get; set; }

    public override string ToString()
        => $"INT({Value})";
}

public class AddOp : IOperation
{
    public override string ToString()
        => "ADD";
}

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