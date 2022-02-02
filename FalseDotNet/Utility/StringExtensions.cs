using System.Text;

namespace FalseDotNet.Utility;

public static class StringExtensions
{
    public static string Escape(this string str, char quote = '"')
    {
        var literal = new StringBuilder(str.Length + 2);
        literal.Append(quote);
        foreach (var c in str)
        {
            switch (c)
            {
                case '\"':
                    literal.Append("\\\"");
                    break;
                case '\\':
                    literal.Append(@"\\");
                    break;
                case '\0':
                    literal.Append(@"\0");
                    break;
                case '\a':
                    literal.Append(@"\a");
                    break;
                case '\b':
                    literal.Append(@"\b");
                    break;
                case '\f':
                    literal.Append(@"\f");
                    break;
                case '\n':
                    literal.Append(@"\n");
                    break;
                case '\r':
                    literal.Append(@"\r");
                    break;
                case '\t':
                    literal.Append(@"\t");
                    break;
                case '\v':
                    literal.Append(@"\v");
                    break;
                default:
                    // ASCII printable character
                    if (c >= 0x20 && c <= 0x7e)
                    {
                        literal.Append(c);
                        // As UTF16 escaped character
                    }
                    else
                    {
                        literal.Append(@"\u");
                        literal.Append(((int)c).ToString("x4"));
                    }

                    break;
            }
        }

        literal.Append(quote);
        return literal.ToString();
    }
}