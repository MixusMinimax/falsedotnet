using FalseDotNet.Operations;

namespace FalseDotNet;

public interface ICodeParser
{
    public IEnumerable<IOperation> Parse(string code);
}