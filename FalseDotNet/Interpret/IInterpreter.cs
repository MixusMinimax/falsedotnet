using FalseDotNet.Parsing;

namespace FalseDotNet.Interpret;

public interface IInterpreter
{
    public void Interpret(Program program, bool printOperations = true);
}