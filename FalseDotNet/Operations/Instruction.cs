namespace FalseDotNet.Operations;

public record Instruction(Operation Op, long Argument = 0)
{
    public static implicit operator Instruction(Operation op)
        => new(op);
}