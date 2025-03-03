using MoonSharp.Interpreter;

namespace Borz.Languages.C;

[MoonSharpUserData]
public class CppProject : CProject
{
    public CppProject(string name, BinType type, string directory) : base(name, type, directory)
    {
        Language = Lang.Cpp;
        StdVersion = "c++17";
    }
    
    
}