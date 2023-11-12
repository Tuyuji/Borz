namespace Borz.Core.Lua;

[BorzUserData]
public class LuaReg
{
    public static void generator(string name, MoonSharp.Interpreter.Closure func)
    {
        Borz.Generators.TryAdd(name, () => { func.Call(); });
    }
}