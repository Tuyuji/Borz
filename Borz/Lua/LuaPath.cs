using MoonSharp.Interpreter;

namespace Borz.Lua;

public class TempSetCwd : IDisposable
{
    private string oldCwd;

    public TempSetCwd(Script script)
    {
        //get system cwd
        oldCwd = Environment.CurrentDirectory;
        //set system cwd to script cwd
        Environment.CurrentDirectory = script.GetCwd();
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = oldCwd;
    }
}

[MoonSharpUserData]
public class LuaPath
{
    public static string combine(Script script, params string[] paths)
    {
        string result;
        using (new TempSetCwd(script))
        {
            result = Path.Combine(paths);
        }

        return result;
    }

    public static string? getFileName(Script script, string path)
    {
        string? result;
        using (new TempSetCwd(script))
        {
            result = Path.GetFileName(path);
        }

        return result;
    }

    public static string getFileNameNoExt(Script script, string path)
    {
        string result;
        using (new TempSetCwd(script))
        {
            result = Path.GetFileNameWithoutExtension(path);
        }

        return result;
    }

    public static string getAbsolute(Script script, string path)
    {
        string result;
        using (new TempSetCwd(script))
        {
            result = Path.GetFullPath(path);
        }

        return result;
    }

    public static string getAbs(Script script, string path)
    {
        return getAbsolute(script, path);
    }
}