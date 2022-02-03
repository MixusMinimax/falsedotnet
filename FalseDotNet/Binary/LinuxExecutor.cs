using System.Diagnostics;
using System.Drawing;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Binary;

public interface ILinuxExecutor
{
    public void Execute(string fileName, string arguments);
    public void Assemble(string inputPath, string outputPath);
    public void Link(string inputPath, string outputPath);
}

public class LinuxExecutor : ILinuxExecutor
{
    private readonly ILogger _logger;

    public LinuxExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public void Execute(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo();

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = @"C:\Windows\system32\bash.exe";
            startInfo.Arguments =
                "-c " + $"{fileName} {arguments}".Escape();
        }
        else if (OperatingSystem.IsLinux())
        {
            startInfo.FileName = fileName;
            startInfo.Arguments = arguments;
        }
        else
        {
            throw new Exception("Operating System not supported!");
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        _logger.WriteLine($"Command: {startInfo.FileName} {startInfo.Arguments}".Pastel(Color.SlateGray));
        var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        _logger.Write(output.Pastel(Color.Gold));
        var err = process.StandardError.ReadToEnd();
        _logger.Write(err.Pastel(Color.IndianRed));

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception($"{fileName} failed! ExitCode: {process.ExitCode}");
        }
    }

    public void Assemble(string inputPath, string outputPath)
    {
        Execute("nasm", $"-felf64 -o \"{outputPath}\" \"{inputPath}\"");
    }

    public void Link(string inputPath, string outputPath)
    {
        Execute("ld", $"-o \"{outputPath}\" \"{inputPath}\"");
    }
}