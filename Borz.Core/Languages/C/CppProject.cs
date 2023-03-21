using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz.Core.Languages.C;

[MoonSharpUserData]
[ProjectLanguage(Language.Cpp)]
public class CppProject : CProject
{
    public CppProject(string name, BinType type, string directory = "", Language language = Language.Cpp) : base(name,
        type, directory, language)
    {
    }

    public static CppProject Create(Script script, string name, BinType type)
    {
        var proj = new CppProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }
}