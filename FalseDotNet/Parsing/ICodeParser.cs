using FalseDotNet.Operations;

namespace FalseDotNet.Parsing;

public interface ICodeParser
{
    public Program Parse(string code);
}