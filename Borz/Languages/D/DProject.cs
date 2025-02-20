using Borz.Languages.C;
using MoonSharp.Interpreter;

namespace Borz.Languages.D;

[MoonSharpUserData]
public class DProject : CProject
{
    public List<string> Versions { get; } = new();
    public PhobosType PhobosType { get; set; } = PhobosType.NotSet;
    
    public DProject(string name, BinType type, string directory) : base(name, type, directory)
    {
        Language = Lang.D;
        StdVersion = String.Empty;
    }

    public void AddVersion(string version)
    {
        Versions.Add(version);
    }

    public void AddVersions(params string[] versions)
    {
        Versions.AddRange(versions);
    }

    public string GetGeneratedIncludeDirectory(Options opt)
    {
        return Path.Combine(GetIntermediateDirectory(opt), "include");
    }
}