namespace FalseDotNet;

public class CodeParser : ICodeParser
{
    public IEnumerable<string> Parse(string code)
    {
        return code.Split();
    }
}