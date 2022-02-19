namespace FalseDotNet.Cli.SubCommandExtensions;

public interface ISubCommand
{
    Task<int> RunAsync(object opts);
}

public abstract class SubCommand<TOptions> : ISubCommand
{
    public Task<int> RunAsync(object opts)
    {
        return RunAsync((TOptions)opts);
    }

    public abstract Task<int> RunAsync(TOptions opts);
}