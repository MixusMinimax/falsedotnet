using System.Text.RegularExpressions;

namespace FalseDotNet.Binary;

public interface IPathConverter
{
    public string ConvertToWsl(string path);
}

public class PathConverter : IPathConverter
{
    public string ConvertToWsl(string path)
    {
        path = path.Replace('\\', '/');
        path = Regex.Replace(path, @"^[A-Z]:", m => "/mnt/" + m.ToString()[..1].ToLower());
        return path;
    }
}