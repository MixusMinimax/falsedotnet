namespace FalseDotNet;

public interface ILogger
{
    public ILogger Write(string message);
    public ILogger WriteLine(string message);
}