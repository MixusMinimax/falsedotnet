using FalseDotNet.Operations;

namespace FalseDotNet;

public interface IInterpreter
{
    public void Interpret(IEnumerable<IOperation> operations, bool printOperations = true);
}