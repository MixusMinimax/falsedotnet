namespace FalseDotNet.Instructions;

public record Instruction(Operation Op, long Argument = 0)
{
    public static implicit operator Instruction(Operation op)
        => new(op);

    public override string ToString()
        => $"{Op}{(Op.HasArgument() ? $", {Argument}" : "")};";
}