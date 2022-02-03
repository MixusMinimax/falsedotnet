using CommandLine;
using FalseDotNet.Cli;
using FalseDotNet.Cli.ParserExtensions;
using FalseDotNet.Compile;
using FalseDotNet.Interpret;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;

return new ServiceCollection()
    .RegisterSubCommands(typeof(Program))
    .AddSingleton<ILogger, DefaultLogger>()
    .AddTransient<IIdGenerator, IncrementingIdGenerator>()
    .AddTransient<ICodeParser, CodeParser>()
    .AddTransient<IInterpreter, Interpreter>()
    .AddTransient<ICompiler, Compiler>()
    .BuildServiceProvider()
    .ParseAndExecute(Parser.Default, args, _ => 1);