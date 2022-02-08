namespace FalseDotNet.Compile.Instructions;

public record Literal(long Value, bool Char = false, ERegisterSize Size = ERegisterSize.r) : IOperand
{
    private string GetValue()
    {
        var mask = 0xfffffffffffffffful;
        mask >>= 8 * (8 - Size.NumBytes());
        var value = (ulong)Value;
        value &= mask;
        var ret = $"{value:X}";
        return ret.PadLeft((ret.Length + 1) / 2 * 2, '0');
    }

    public override string ToString()
        => Char
            ? $"'{(char)Value}'"
            : $"0x{GetValue()}";
}