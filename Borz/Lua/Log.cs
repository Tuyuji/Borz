using MoonSharp.Interpreter;

namespace Borz.Lua;

[MoonSharpUserData]
public class Log
{
    public static void debug(string message)
    {
        MugiLog.Debug(message);
    }

    public static void info(string message)
    {
        MugiLog.Info(message);
    }

    public static void warning(string message)
    {
        MugiLog.Warning(message);
    }

    public static void warn(string message)
    {
        MugiLog.Warning(message);
    }

    public static void error(string message)
    {
        MugiLog.Error(message);
    }

    public static void fatal(string message)
    {
        throw new Exception(message);
    }
}