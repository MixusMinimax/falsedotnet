using FalseDotNet.Instructions;

namespace FalseDotNet.Parse;

public record Program(
    long EntryId,
    IReadOnlyDictionary<long, IReadOnlyList<Instruction>> Functions,
    IReadOnlyDictionary<long, string> Strings 
);