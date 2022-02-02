using FalseDotNet.Operations;

namespace FalseDotNet.Parsing;

public record Program(
    long EntryId,
    IReadOnlyDictionary<long, IReadOnlyList<Instruction>> Functions,
    IReadOnlyDictionary<long, string> Strings 
);