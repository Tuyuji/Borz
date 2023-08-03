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

    public static void copy(Script script, string src, string dest)
    {
        var srcAbs = script.GetAbsolute(src);
        var destAbs = script.GetAbsolute(dest);

        if (!Directory.Exists(srcAbs))
            throw new Exception($"Source directory {srcAbs} does not exist.");

        if (!Directory.Exists(destAbs))
            Directory.CreateDirectory(destAbs);

        Util.CopyFilesRecursively(srcAbs, destAbs);
    }

    //Sometimes things you download might have some screwy time stamps
    //so this will help you fix them.
    public static void recursiveFixModifyTimes(Script script, string directory)
    {
        var absPath = script.GetAbsolute(directory);
        //Recursive all the way down
        string[] files = Directory.GetFiles(absPath, "*", SearchOption.AllDirectories);
        foreach (var file in files) File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
    }
}