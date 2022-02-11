namespace FalseDotNet.Commands;

public record Command(Operation Op, long Argument = 0)
{
    public static implicit operator Command(Operation op)
        => new(op);

    public override string ToString()
        => $"{Op}{(Op.HasArgument() ? $", {Argument}" : "")}";
}