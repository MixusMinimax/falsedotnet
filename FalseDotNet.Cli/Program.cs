using CommandLine;
using FalseDotNet.Binary;
using FalseDotNet.Cli;
using FalseDotNet.Cli.SubCommandExtensions;
using FalseDotNet.Compile;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Interpret;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;

return new ServiceCollection()
    .RegisterSubCommands(typeof(Program))
    .AddSingleton<ILogger, DefaultLogger>()
    .AddTransient<IIdGenerator, IncrementingIdGenerator>()
    .AddSingleton<IPathConverter, PathConverter>()
    .AddSingleton<ILinuxExecutor, LinuxExecutor>()
    .AddTransient<ICodeParser, CodeParser>()
    .AddTransient<IInterpreter, Interpreter>()
    .AddTransient<ICompiler, Compiler>()
    .AddTransient<IOptimizer, Optimizer>()
    .BuildServiceProvider()
    .ParseAndExecute(new Parser(with => with.CaseInsensitiveEnumValues = true), args, _ => 1);