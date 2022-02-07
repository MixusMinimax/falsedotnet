namespace FalseDotNet.Compile.Instructions;

public record LabelStatement(string LabelName) : IAsmLine
{
    public override string ToString()
        => $"{LabelName}:";
}