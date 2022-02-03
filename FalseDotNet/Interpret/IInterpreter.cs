using FalseDotNet.Parse;

namespace FalseDotNet.Interpret;

public interface IInterpreter
{
    public void Interpret(Program program, bool printOperations = true);
}