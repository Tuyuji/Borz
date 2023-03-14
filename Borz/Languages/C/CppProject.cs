using Borz.Lua;
using MoonSharp.Interpreter;

namespace Borz.Languages.C;
[MoonSharpUserData]
public class CppProject : CProject
{
    public CppProject(string name, BinType type, string directory = "", Language language = Language.Cpp) : base(name, type, directory, language)
    {
    }
    
    public static CppProject New(Script script, string name, BinType type)
    {
        var proj = new CppProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }
}