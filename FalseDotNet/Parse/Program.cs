using FalseDotNet.Commands;

namespace FalseDotNet.Parse;

public record Program(
    long EntryId,
    IReadOnlyDictionary<long, IReadOnlyList<Command>> Functions,
    IReadOnlyDictionary<long, string> Strings 
);