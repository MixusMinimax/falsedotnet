using CommandLine;
using FalseDotNet;
using FalseDotNet.Cli;
using FalseDotNet.Cli.ParserExtensions;
using FalseDotNet.Interpret;
using FalseDotNet.Parsing;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;

return new ServiceCollection()
    .RegisterSubCommands(typeof(Program))
    .AddSingleton<ILogger, DefaultLogger>()
    .AddTransient<IIdGenerator, IncrementingIdGenerator>()
    .AddTransient<ICodeParser, CodeParser>()
    .AddTransient<IInterpreter, Interpreter>()
    .BuildServiceProvider()
    .ParseAndExecute(Parser.Default, args, _ => 1);