using System.Diagnostics;

namespace Borz.Core;

public static class UnixUtil
{
    public record RunOutput(string Ouput, string Error, int Exitcode);

    public static RunOutput RunCmd(string command, string args, string workingDir = "")
    {
        var output = string.Empty;
        var error = string.Empty;
        var proc = Process.Start(new ProcessStartInfo(command, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = workingDir
        });
        if (proc == null) return new RunOutput(string.Empty, string.Empty, 1);
        proc.OutputDataReceived += (sender, eventArgs) => { output += eventArgs.Data; };
        proc.ErrorDataReceived += (sender, eventArgs) => { error += eventArgs.Data + "\n"; };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        return new RunOutput(
            output,
            error,
            proc.ExitCode);
    }

    public static int GetUserId()
    {
        return GetUserId(Environment.UserName);
    }

    public static int GetUserId(string username)
    {
        return int.Parse(RunCmd("id", "-u " + username).Ouput);
    }
}