namespace FalseDotNet.Compile.Instructions;

public record Verbatim(string Line) : IAsmLine, IOperand
{
    public override string ToString()
        => Line;
}