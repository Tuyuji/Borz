using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpUserData]
public class LuaDir
{
    public static bool create(Script script, string dir)
    {
        dir = script.GetAbsolute(dir);
        return Directory.CreateDirectory(dir).Exists;
    }

    public static bool exists(Script script, string dir)
    {
        dir = script.GetAbsolute(dir);
        return Directory.Exists(dir);
    }

    public static void delete(Script script, string dir, bool recursive = true)
    {
        dir = script.GetAbsolute(dir);
        Directory.Delete(dir, recursive);
    }

    //List dirs only list top level directories
    public static string[] listDirs(Script script, string dir)
    {
        dir = script.GetAbsolute(dir);
        return Directory.GetDirectories(dir);
    }

    public static string[] listDirs(Script script)
    {
        return Directory.GetDirectories(script.GetCwd());
    }
}