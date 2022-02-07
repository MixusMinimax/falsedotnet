namespace FalseDotNet.Compile.Instructions;

public record Literal(long Value) : IOperand
{
    public override string ToString()
        => $"0x{Value:X}";
}