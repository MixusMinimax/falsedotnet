namespace FalseDotNet.Compile.Instructions;

public enum ESection
{
    Text,
    Data,
    RoData,
    Bss
}

public record Section(ESection SectionName) : IAsmLine
{
    public override string ToString()
        => $"    section .{SectionName.ToString().ToLower()}";
}