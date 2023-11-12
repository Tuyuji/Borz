using Borz.Core.Languages.C;
using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz.Core.Languages.D;

[MoonSharpUserData]
[ProjectLanguage(Core.Language.D)]
public class DProject : CProject
{
    public List<string> Versions { get; } = new();
    public List<DubPkg> Packages { get; } = new();
    public PhobosType PhobosType { get; set; } = PhobosType.NotSet;

    public DProject(string name, BinType type, string directory = "", bool addToWorkspace = true) :
        base(name, type, directory, Core.Language.D)
    {
        StdVersion = string.Empty;
    }

    [MoonSharpHidden]
    public static DProject Create(Script script, string name, BinType type)
    {
        var proj = new DProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }

    public void AddVersion(string version)
    {
        Versions.Add(version);
    }

    public void AddVersions(params string[] versions)
    {
        Versions.AddRange(versions);
    }

    public void AddPackage(DubPkg pkg)
    {
        Packages.Add(pkg);
    }

    public void AddPackages(params DubPkg[] pkgs)
    {
        Packages.AddRange(pkgs);
    }
}