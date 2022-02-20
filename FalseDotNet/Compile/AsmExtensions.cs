using FalseDotNet.Compile.Instructions;

namespace FalseDotNet.Compile;

public static class AsmExtensions
{
    public static Asm Ins(this Asm asm, Mnemonic mnemonic, params IOperand[] operands)
        => asm.WriteLine(new Instruction(mnemonic, operands));

    public static Asm Com(this Asm asm, string message, bool indent = false)
        => asm.WriteLine(new Comment(message, indent));

    public static Asm Str(this Asm asm, string line = "")
        => asm.WriteLine(new Verbatim(line));

    public static Asm Sec(this Asm asm, ESection section)
        => asm.WriteLine(new Section(section));

    public static Asm Lbl(this Asm asm, string label)
        => asm.WriteLine(new LabelStatement(label));

    public static Asm Lea(this Asm asm, Register destination, string label)
        => asm.Ins(Mnemonic.Lea, destination, new LabelAddress(label));
    
    public static Asm Lea(this Asm asm, Register destination, IOperand operand)
        => asm.Ins(Mnemonic.Lea, destination, operand);

    public static Asm Mov(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.Mov, destination, source);

    public static Asm Mov(this Asm asm, IOperand destination, string source)
        => asm.Ins(Mnemonic.Mov, destination, new Verbatim(source));

    public static Asm Mov(this Asm asm, Register destination, long source)
        => asm.Ins(Mnemonic.Mov, destination, new Literal(source, Size: destination.Size));

    public static Asm Mov(this Asm asm, Register destination, char source)
        => asm.Ins(Mnemonic.Mov, destination, new Literal(source, true));

    public static Asm Zro(this Asm asm, Register destination)
        => asm.Ins(Mnemonic.Xor, destination, destination);

    public static Asm Add(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.Add, destination, source);

    public static Asm Add(this Asm asm, Register destination, long source)
        => asm.Ins(Mnemonic.Add, destination, new Literal(source, Size: destination.Size));

    public static Asm Add(this Asm asm, Register destination, char source)
        => asm.Ins(Mnemonic.Add, destination, new Literal(source, true));

    public static Asm Sub(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.Sub, destination, source);

    public static Asm Neg(this Asm asm, IOperand destination)
        => asm.Ins(Mnemonic.Neg, destination);

    public static Asm Mul(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.IMul, destination, source);

    public static Asm Div(this Asm asm, IOperand divisor)
        => asm.Ins(Mnemonic.IDiv, divisor);

    public static Asm Inc(this Asm asm, Register register)
        => asm.Ins(Mnemonic.Inc, register);

    public static Asm Dec(this Asm asm, Register register)
        => asm.Ins(Mnemonic.Dec, register);

    public static Asm And(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.And, destination, source);

    public static Asm And(this Asm asm, IOperand destination, long source)
        => asm.Ins(Mnemonic.And, destination, new Literal(source));

    public static Asm Or(this Asm asm, IOperand destination, IOperand source)
        => asm.Ins(Mnemonic.Or, destination, source);

    public static Asm Not(this Asm asm, IOperand destination)
        => asm.Ins(Mnemonic.Not, destination);

    public static Asm Syscall(this Asm asm)
        => asm.Ins(Mnemonic.Syscall);

    public static Asm Cmp(this Asm asm, IOperand a, IOperand b)
        => asm.Ins(Mnemonic.Cmp, a, b);

    public static Asm Cmp(this Asm asm, Register a, long b)
        => asm.Ins(Mnemonic.Cmp, a, new Literal(b, Size: a.Size));

    public static Asm Cll(this Asm asm, string label)
        => asm.Ins(Mnemonic.Call, new LabelOperand(label));

    public static Asm Jmp(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jmp, new LabelOperand(label));

    public static Asm Je(this Asm asm, string label)
        => asm.Ins(Mnemonic.Je, new LabelOperand(label));

    public static Asm Jne(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jne, new LabelOperand(label));

    public static Asm Jge(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jge, new LabelOperand(label));
    
    public static Asm Jg(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jg, new LabelOperand(label));
    
    public static Asm Jl(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jl, new LabelOperand(label));

    public static Asm Jz(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jz, new LabelOperand(label));

    public static Asm Jnz(this Asm asm, string label)
        => asm.Ins(Mnemonic.Jnz, new LabelOperand(label));
}