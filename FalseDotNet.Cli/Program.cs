using CommandLine;
using FalseDotNet;
using FalseDotNet.Cli;
using FalseDotNet.Cli.ParserExtensions;
using Microsoft.Extensions.DependencyInjection;

return new ServiceCollection()
    .RegisterSubCommands(typeof(Program))
    .AddSingleton<ICodeParser, CodeParser>()
    .AddSingleton<ILogger, DefaultLogger>()
    .AddTransient<IInterpreter, Interpreter>()
    .BuildServiceProvider()
    .ParseAndExecute(Parser.Default, args, _ => 1);