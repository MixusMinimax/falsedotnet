using System.Text.RegularExpressions;

namespace FalseDotNet.Binary;

public interface IPathConverter
{
    public FileInfo ConvertToWsl(FileInfo path);
}

public partial class PathConverter : IPathConverter
{
    public FileInfo ConvertToWsl(FileInfo path)
    {
        var stringPath = path.ToString();
        stringPath = stringPath.Replace('\\', '/');
        stringPath = PathRegex().Replace(stringPath, m => "/mnt/" + m.ToString()[..1].ToLower());
        return new FileInfo(stringPath);
    }

    [GeneratedRegex(@"^[A-Z]:(?=[/\\])")]
    private static partial Regex PathRegex();
}
