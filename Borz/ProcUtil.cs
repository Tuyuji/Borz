using System.Collections.Specialized;
using System.Diagnostics;

namespace Borz;

public static class ProcUtil
{
    public record RunOutput(string Ouput, string Error, int Exitcode);

    public static RunOutput RunCmd(string command, string args, string workingDir = "", IDictionary<string, string?>? env = null)
    {
        var output = string.Empty;
        var error = string.Empty;
        var startInfo = new ProcessStartInfo(command, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = workingDir
        };
        if (env != null)
        {
            foreach (var envVar in env)
            {
                startInfo.Environment.Add(envVar);
            }
        }
        var proc = Process.Start(startInfo);
        if (proc == null) return new RunOutput(string.Empty, string.Empty, 1);
        proc.OutputDataReceived += (sender, eventArgs) => { output += eventArgs.Data + "\n"; };
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

    //RunCmd but supports just writing to the log if a bool is set.
    public static RunOutput RunCmdOptLog(string command, string args, string workingDir, bool justlog)
    {
        if (!justlog)
            return RunCmd(command, args, workingDir);

        MugiLog.Info($"{command} {args}");
        return new RunOutput(string.Empty, string.Empty, 0);
    }

    //RunCmdOptLog but respects the machines preferred binary.
    public static RunOutput RunCmdOptLog(MachineInfo info, string command, string args, string workingDir, bool justlog)
    {
        return RunCmdOptLog(info.Binaries.GetValueOrDefault(command, command), args, workingDir, justlog);
    }
}