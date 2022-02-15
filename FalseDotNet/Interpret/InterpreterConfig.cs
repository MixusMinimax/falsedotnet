using FalseDotNet.Utility;

namespace FalseDotNet.Interpret;

public record InterpreterConfig
{
    public bool PrintOperations { get; set; }
    public TypeSafety TypeSafety { get; set; } = TypeSafety.None;
}