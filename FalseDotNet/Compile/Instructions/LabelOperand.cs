namespace FalseDotNet.Compile.Instructions;

public record LabelOperand(string Label) : IOperand
{
    public override string ToString()
        => Label;
}