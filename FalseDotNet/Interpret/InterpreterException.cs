namespace FalseDotNet.Interpret;

public class InterpreterException : Exception
{
    public InterpreterException(string message = "") : base(message)
    { }
}