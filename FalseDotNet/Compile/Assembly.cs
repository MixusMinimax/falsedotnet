using FalseDotNet.Compile.Instructions;

namespace FalseDotNet.Compile;

public class Assembly
{
    public readonly IEnumerable<IAsmLine> Lines = new List<IAsmLine>();
}