namespace FalseDotNet.Compilation;

public class CompilerException : Exception
{
    public CompilerException(string message = "") : base(message)
    { }
}