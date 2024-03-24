namespace Borz.Languages.C;

public class CppProject : CProject
{
    public CppProject(string name, BinType type, string directory) : base(name, type, directory)
    {
        Language = Lang.Cpp;
        StdVersion = "17";
    }
    
    
}