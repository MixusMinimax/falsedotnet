using System.Diagnostics.CodeAnalysis;

namespace FalseDotNet.Compile.Instructions;

public record Instruction(Mnemonic Mnemonic, params IOperand[] Operands) : IAsmLine
{
    public IReadOnlyList<ERegister> ModifiedRegisters
        => _modifiedRegisters ?? GetModifiedRegisters();

    private List<ERegister>? _modifiedRegisters;

    private List<ERegister> GetModifiedRegisters()
    {
        _modifiedRegisters = new List<ERegister>();
        switch (Mnemonic)
        {
            case >= Mnemonic.Mov and <= Mnemonic.IMul
                or >= Mnemonic.And and <= Mnemonic.Not
                or >= Mnemonic.CMovE and <= Mnemonic.CMovL:
                _modifiedRegisters.Add(((Register)Operands[0]).Name);
                break;

            case Mnemonic.IDiv:
                _modifiedRegisters.Add(ERegister.ax);
                _modifiedRegisters.Add(ERegister.dx);
                break;

            case Mnemonic.Cmp:
                break;

            case Mnemonic.Syscall:
                _modifiedRegisters.Add(ERegister.ax);
                _modifiedRegisters.Add(ERegister.cx);
                _modifiedRegisters.Add(ERegister.r11);
                break;
        }

        return _modifiedRegisters;
    }

    public override string ToString()
        => $"    {Mnemonic.ToString().ToLower()} {string.Join<IOperand>(", ", Operands)}".TrimEnd();
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Mnemonic
{
    Mov,

    // Arithmetic
    Add,
    Sub,
    Neg,
    IMul,
    IDiv,

    // Logic
    And,
    Or,
    Xor,
    Not,

    Cmp,

    // Jump
    Jmp,
    Jne,
    Je,
    Jnz,
    Jz,

    // Conditional Move
    CMovE,
    CMovL,

    Syscall
}