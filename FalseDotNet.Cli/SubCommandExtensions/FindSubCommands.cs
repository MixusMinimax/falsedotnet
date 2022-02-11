using System.Drawing;
using CommandLine;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pastel;

namespace FalseDotNet.Cli.SubCommandExtensions;

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
            where (type.BaseType?.IsGenericType ?? false) &&
                  type.BaseType?.GetGenericTypeDefinition() == typeof(SubCommand<>)
            select (
                SubCommand: type,
                Options: type
                    .BaseType!
                    .GetGenericArguments()[0]
            )
        ).ToDictionary(e => e.Options);

        services.TryAdd(
            from command in subCommands.Values
            select new ServiceDescriptor(command.SubCommand, command.SubCommand, ServiceLifetime.Singleton)
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
        if (services.GetRequiredService(commandType) is ISubCommand command) return command.Run(parsed.Value);
        services.GetRequiredService<ILogger>()
            .WriteLine($"Command [{commandType.Name}] not found!".Pastel(Color.IndianRed));
        return 1;
    }
}