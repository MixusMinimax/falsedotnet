namespace FalseDotNet.Compile;

public class CompilerException : Exception
{
    public CompilerException(string message = "") : base(message)
    { }
}