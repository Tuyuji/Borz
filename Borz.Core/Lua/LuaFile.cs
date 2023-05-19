using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpUserData]
public class LuaFile
{
    public static void delete(Script script, string file)
    {
        file = script.GetAbsolute(file);
        File.Delete(file);
    }

    public static bool exists(Script script, string file)
    {
        file = script.GetAbsolute(file);
        return File.Exists(file);
    }
}