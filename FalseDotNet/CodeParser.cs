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
            var value = (long)(character - '0');
            while (characters.Count > 0 && characters.First() is >= '0' and <= '9')
            {
                var c = characters.PopFront();
                value = value * 10 + (c - '0');
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
                if (characters.Count == 0) throw new CodeParserException("Missing character");
                return Op.Int(characters.PopFront());

            case '$':
                return Op.Dup;

            case '%':
                return Op.Drop;

            case '\\':
                return Op.Swap;

            case '@':
                return Op.Rot;

            case 'ø':
                return Op.Pick;

            case '+':
                return Op.Add;

            case '-':
                return Op.Sub;

            case '*':
                return Op.Mul;

            case '/':
                return Op.Div;

            case '_':
                return Op.Neg;

            case '&':
                return Op.And;

            case '|':
                return Op.Or;

            case '~':
                return Op.Not;

            case '>':
                return Op.Gt;

            case '=':
                return Op.Eq;
            
            case >= 'a' and <= 'z':
                return Op.Ref(character - 'a');
            
            case ':':
                return Op.Store;
            
            case ';':
                return Op.Load;

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