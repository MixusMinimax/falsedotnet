using FalseDotNet.Operations;
using FalseDotNet.Utility;

namespace FalseDotNet;

using Operations;

public class CodeParser : ICodeParser
{
    private IOperation? ParseOperation(LinkedList<char> characters)
    {
        if (characters.Count == 0) return null;
        var character = characters.PopFront();

        if (char.IsNumber(character))
        {
            var value = (ulong)(character - '0');
            while (characters.Count > 0 && characters.First() is >= '0' and <= '9')
            {
                var c = characters.PopFront();
                value = value * 10ul + (ulong)(c - '0');
            }

            return Op.Int(value);
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
                throw new NotImplementedException();

            case '+':
                return Op.Add;

            case '.':
                return Op.OutputDecimal;

            case ',':
                return Op.OutputChar;
        }


        return null;
    }

    public IEnumerable<IOperation> Parse(string code)
    {
        var characters = new LinkedList<char>(code);
        var operations = new LinkedList<IOperation>();
        while (characters.Count != 0)
        {
            var op = ParseOperation(characters);
            if (op is not null)
                operations.AddLast(op);
        }

        return operations;
    }
}