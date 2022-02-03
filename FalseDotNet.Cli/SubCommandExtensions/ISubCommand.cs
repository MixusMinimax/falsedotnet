namespace FalseDotNet.Cli.SubCommandExtensions;

public interface ISubCommand<in TOptions>
{
    int Run(TOptions opts);
}