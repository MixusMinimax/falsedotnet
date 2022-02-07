namespace FalseDotNet.Compile.Instructions;

public record Comment(string Message, bool Indent = false) : IAsmLine
{
    public override string ToString()
    {
        return (Indent ? "    ; " : "; ") + Message;
    }
}