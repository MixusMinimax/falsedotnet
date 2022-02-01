using FalseDotNet.Operations;
using FalseDotNet.Utility;

namespace FalseDotNet.Parsing;

public class CodeParser : ICodeParser
{
    private readonly IIdGenerator _idGenerator;

    public CodeParser(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    private Instruction? ParseOperation(LinkedList<char> characters)
    {
        if (characters.Count == 0) return null;
        var character = characters.PopFront();

        if (char.IsNumber(character))
        {
            var value = (long)(character - '0');
            while (characters.Count > 0 && characters.First() is >= '0' and <= '9')
            {
                var c = characters.PopFront();
                value = value * 10 + (c - '0');
            }

            return new Instruction(Operation.IntLiteral, value);
        }

        switch (character)
        {
            case '{':
                do
                {
                    if (characters.Count == 0) throw new CodeParserException("Missing '}'");
                } while (characters.PopFront() != '}');

                return null;

            case '\'':
                if (characters.Count == 0) throw new CodeParserException("Missing character");
                return new Instruction(Operation.IntLiteral, characters.PopFront());

            case '$':
                return Operation.Dup;

            case '%':
                return Operation.Drop;

            case '\\':
                return Operation.Swap;

            case '@':
                return Operation.Rot;

            case 'ø':
                return Operation.Pick;

            case '+':
                return Operation.Add;

            case '-':
                return Operation.Sub;

            case '*':
                return Operation.Mul;

            case '/':
                return Operation.Div;

            case '_':
                return Operation.Neg;

            case '&':
                return Operation.And;

            case '|':
                return Operation.Or;

            case '~':
                return Operation.Not;

            case '>':
                return Operation.Gt;

            case '=':
                return Operation.Eq;

            case '[':
                return new Instruction(Operation.Lambda, _idGenerator.NewId);

            case ']':
                return Operation.Ret;

            case '!':
                return Operation.Execute;

            case '?':
                return Operation.ConditionalExecute;

            case '#':
                return Operation.While;

            case >= 'a' and <= 'z':
                return new Instruction(Operation.Ref, character - 'a');

            case ':':
                return Operation.Store;

            case ';':
                return Operation.Load;

            case '.':
                return Operation.OutputDecimal;

            case ',':
                return Operation.OutputChar;
        }


        return null;
    }

    public Program Parse(string code)
    {
        var characters = new LinkedList<char>(code);

        var lambdaIds = new Stack<long>();
        var lambdas = new Dictionary<long, LinkedList<Instruction>>();
        LinkedList<Instruction> instructions, program = instructions = new LinkedList<Instruction>();

        while (characters.Count != 0)
        {
            var instruction = ParseOperation(characters);
            if (instruction is null) continue;
            instructions.AddLast(instruction);

            if (instruction.Op is Operation.Lambda)
            {
                lambdaIds.Push(instruction.Argument);
                lambdas[instruction.Argument] = instructions = new LinkedList<Instruction>();
            }
            else if (instruction.Op is Operation.Ret)
            {
                lambdaIds.Pop();
                instructions = lambdaIds.TryPeek(out var id)
                    ? lambdas[id]
                    : program;
            }
        }

        return new Program(
            new List<Instruction>(program),
            lambdas.ToDictionary(
                e => e.Key,
                e => (IReadOnlyList<Instruction>)e.Value.ToList()
            )
        );
    }
}