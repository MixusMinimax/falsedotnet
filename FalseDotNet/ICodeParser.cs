namespace FalseDotNet;

public interface ICodeParser
{
    public IEnumerable<string> Parse(string code);
}