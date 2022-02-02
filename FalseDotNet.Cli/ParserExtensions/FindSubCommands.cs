using System.Drawing;
using System.Reflection;
using CommandLine;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pastel;

namespace FalseDotNet.Cli.ParserExtensions;

public static class FindSubCommands
{
    private class SubCommandsDict
    {
        public readonly IDictionary<Type, (Type SubCommand, Type Options)> SubCommands;

        public SubCommandsDict(IDictionary<Type, (Type SubCommand, Type Options)> subCommands)
        {
            SubCommands = subCommands;
        }
    }

    public static IServiceCollection RegisterSubCommands(this IServiceCollection services, params Type[] markers)
    {
        var subCommands = (
            from marker in markers
            from type in marker.Assembly.ExportedTypes
            where !type.IsInterface && !type.IsAbstract
            where type
                .GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubCommand<>))
            select (
                SubCommand: type,
                Options: type
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubCommand<>))
                    .GetGenericArguments()[0]
            )
        ).ToDictionary(e => e.Options);

        services.TryAdd(
            from command in subCommands.Values
            select new ServiceDescriptor(command.SubCommand, command.SubCommand, ServiceLifetime.Transient)
        );

        return services.AddSingleton(new SubCommandsDict(subCommands));
    }

    public static int ParseAndExecute(this IServiceProvider services,
        Parser parser, IEnumerable<string> args, Func<IEnumerable<Error>, int> onError)
    {
        var subCommands = services.GetRequiredService<SubCommandsDict>().SubCommands;

        var result = parser.ParseArguments(args, subCommands.Keys.ToArray());

        if (result is not Parsed<object> parsed) return onError(((NotParsed<object>)result).Errors);

        var optionsType = parsed.Value.GetType();
        var commandType = subCommands[optionsType].SubCommand;
        var command = services.GetRequiredService(commandType);
        var func = commandType.GetMethods().First(m =>
            m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == optionsType &&
            m.Name == typeof(ISubCommand<>).GetMethods()[0].Name);
        try
        {
            return (int)(func.Invoke(command, new[] { parsed.Value }) ?? 1);
        }
        catch (TargetInvocationException exception)
        {
            var e = exception.InnerException!;
            services.GetRequiredService<ILogger>().WriteLine(e.ToString().Pastel(Color.IndianRed));
            return 1;
        }
    }
}