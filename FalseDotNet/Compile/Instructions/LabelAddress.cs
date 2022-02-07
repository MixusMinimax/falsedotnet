namespace FalseDotNet.Compile.Instructions;

public record LabelAddress(string Label) : IOperand
{
    public override string ToString()
        => $"[rel {Label}]";
}