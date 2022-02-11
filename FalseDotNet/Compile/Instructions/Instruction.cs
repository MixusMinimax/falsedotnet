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

            case Mnemonic.Cmp or Mnemonic.Ret:
                break;

            case Mnemonic.Push:
                _modifiedRegisters.Add(ERegister.sp);
                break;

            case Mnemonic.Pop:
                _modifiedRegisters.Add(ERegister.sp);
                _modifiedRegisters.Add(((Register)Operands[0]).Name);
                break;

            case Mnemonic.Call:
                _modifiedRegisters.AddRange(Enum.GetValues<ERegister>());
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
    Lea,

    // Arithmetic
    Add,
    Sub,
    Neg,
    Inc,
    Dec,
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
    Jge,
    Jne,
    Je,
    Jnz,
    Jz,

    // Conditional Move
    CMovE,
    CMovL,

    // Memory
    Push,
    Pop,

    // Functions
    Call,
    Syscall,

    Ret,
}