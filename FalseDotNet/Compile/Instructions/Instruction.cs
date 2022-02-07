namespace FalseDotNet.Compile.Instructions;

public interface IInstruction : IAsmLine
{
    public IReadOnlyList<ERegister> ClobberedRegisters { get; }
    public IReadOnlyList<IOperand> Operands { get; }
}