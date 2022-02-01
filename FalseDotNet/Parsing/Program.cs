using FalseDotNet.Operations;

namespace FalseDotNet.Parsing;

public record Program(
    IReadOnlyList<Instruction> Instructions,
    IReadOnlyDictionary<long, IReadOnlyList<Instruction>> Lambdas
);