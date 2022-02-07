namespace FalseDotNet.Utility;

public interface IFlushableLogger : ILogger
{
    public void Flush();
}