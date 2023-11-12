using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz.Core.Languages.C;

[MoonSharpUserData]
[ProjectLanguage(Core.Language.Cpp)]
public class CppProject : CProject
{
    public CppProject(string name, BinType type, string directory = "", string language = Core.Language.Cpp) : base(
        name,
        type, directory, language)
    {
        StdVersion = "17";
    }

    [MoonSharpHidden]
    public static CppProject Create(Script script, string name, BinType type)
    {
        var proj = new CppProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }
}