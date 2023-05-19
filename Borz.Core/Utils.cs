namespace Borz.Core;

public static class Utils
{
    public static T? CreateInstance<T>(this Type type, params object?[]? args)
    {
        return (T?)Activator.CreateInstance(type, args);
    }

    public static UnixUtil.RunOutput RunCmd(string command, string args, string workingDir = "", bool _justLog = false)
    {
        Borz.BuildLog.Enqueue($"RunCmd: {command} {args}");
        if (!_justLog) return UnixUtil.RunCmd(command, args, workingDir);
        MugiLog.Info($"{command} {args}");
        return new UnixUtil.RunOutput(String.Empty, String.Empty, 0);
    }
}