using FalseDotNet.Compile.Instructions;

namespace FalseDotNet.Compile;

public class Asm
{
    private readonly List<IAsmLine> Lines = new();

    public void WriteOut(TextWriter output)
    {
        foreach (var line in Lines)
        {
            output.WriteLine(line.ToString());
        }
    }

    public Asm WriteLine(IAsmLine line)
    {
        Lines.Add(line);
        return this;
    }
}