namespace FalseDotNet.Compile.Instructions;

public interface IInstruction
{
    public IReadOnlyList<Register> ClobberedRegisters { get; }
}