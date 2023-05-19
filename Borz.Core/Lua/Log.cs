using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpUserData]
public class Log
{
    public static void Debug(string message) => MugiLog.Debug(message);
    public static void Info(string message) => MugiLog.Info(message);
    public static void Warning(string message) => MugiLog.Warning(message);
    public static void Warn(string message) => MugiLog.Warning(message);
    public static void Error(string message) => MugiLog.Error(message);

    public static void Fatal(string message)
    {
        throw new Exception(message);
    }
}