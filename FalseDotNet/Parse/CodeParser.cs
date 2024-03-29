﻿using System.Text;
using System.Text.RegularExpressions;
using FalseDotNet.Commands;
using FalseDotNet.Utility;

namespace FalseDotNet.Parse;

public class CodeParser : ICodeParser
{
    private readonly IIdGenerator _lambdaIdGenerator;
    private readonly IIdGenerator _stringIdGenerator;
    private readonly Dictionary<long, string> _strings = new();

    public CodeParser(IIdGenerator lambdaIdGenerator, IIdGenerator stringIdGenerator)
    {
        _lambdaIdGenerator = lambdaIdGenerator;
        _stringIdGenerator = stringIdGenerator;
    }

    private Command? ParseOperation(LinkedList<char> characters)
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

            return new Command(Operation.IntLiteral, value);
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
                return new Command(Operation.IntLiteral, characters.PopFront());

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
                return new Command(Operation.Lambda, _lambdaIdGenerator.NewId);

            case ']':
                return Operation.Ret;

            case '!':
                return Operation.Execute;

            case '?':
                return Operation.ConditionalExecute;

            case '#':
                return Operation.WhileInit;

            case >= 'a' and <= 'z':
                return new Command(Operation.Ref, character - 'a');

            case ':':
                return Operation.Store;

            case ';':
                return Operation.Load;

            case '"':
                var str = new StringBuilder();
                while (characters.Count > 0)
                {
                    var c = characters.PopFront();
                    if (c is '"') break;
                    str.Append(c);
                }

                var id = _stringIdGenerator.NewId;
                _strings[id] = str.ToString();
                return new Command(Operation.PrintString, id);
            
            case '^':
                return Operation.ReadChar;
            
            case '.':
                return Operation.OutputDecimal;

            case ',':
                return Operation.OutputChar;
            
            case 'ß':
                return Operation.Flush;
        }
        
        return null;
    }

    public Program Parse(string code)
    {
        code = Regex.Replace(code, @"\r\n", "\n");
        var characters = new LinkedList<char>(code);

        var lambdaIds = new Stack<long>();
        var lambdas = new Dictionary<long, LinkedList<Command>>();
        var instructions = new LinkedList<Command>();
        var entryId = _lambdaIdGenerator.NewId;
        lambdaIds.Push(entryId);
        lambdas[entryId] = instructions;

        while (characters.Count != 0)
        {
            var instruction = ParseOperation(characters);
            if (instruction is null) continue;
            instructions.AddLast(instruction);

            if (instruction.Op is Operation.WhileInit)
            {
                instructions.AddLast(Operation.WhileCondition);
                instructions.AddLast(Operation.WhileBody);
            }
            else if (instruction.Op is Operation.Lambda)
            {
                lambdaIds.Push(instruction.Argument);
                lambdas[instruction.Argument] = instructions = new LinkedList<Command>();
            }
            else if (instruction.Op is Operation.Ret)
            {
                lambdaIds.Pop();
                instructions = lambdaIds.TryPeek(out var id)
                    ? lambdas[id]
                    : throw new CodeParserException("Ret from empty stack!");
            }
        }

        lambdas[entryId].AddLast(Operation.Exit);

        if (lambdaIds.Count != 1)
            throw new CodeParserException("Unbalanced Lambdas! (Missing ']')");

        return new Program(
            entryId,
            lambdas.ToDictionary(
                e => e.Key,
                e => (IReadOnlyList<Command>)e.Value.ToList()
            ),
            _strings
        );
    }
}