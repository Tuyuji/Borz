namespace Borz.PkgConfig;

public class PkgConfigProject : Project
{
    public PkgConfigInfo PkgConfigInfo { get; }

    public readonly string[] LibraryPaths;
    public readonly string[] Libraries;
    public readonly string[] IncludePaths;
    public readonly IReadOnlyDictionary<string, string?> Defines;

    public PkgConfigProject(PkgConfigInfo info) : base(info.Name, BinType.Unknown, Language.Unknown, "unknown", false)
    {
        this.PkgConfigInfo = info;

        //PkgConfig does provide library paths in CFlags, so we need to parse them out, it uses standard gcc syntax

        var libPaths = new List<string>();
        var libs = new List<string>();
        var includePaths = new List<string>();
        var defines = new Dictionary<string, string?>();
        foreach (var flag in PkgConfigInfo.CFlags)
        {
            if (flag.StartsWith("-L"))
            {
                libPaths.Add(flag[2..]);
            }
            else if (flag.StartsWith("-I"))
            {
                includePaths.Add(flag[2..]);
            }
            else if (flag.StartsWith("-D"))
            {
                var define = flag[2..];
                var split = define.Split('=');
                if (split.Length == 2)
                {
                    defines[split[0]] = split[1];
                }
                else
                {
                    defines[split[0]] = null;
                }
            }
        }

        foreach (var lib in PkgConfigInfo.Libs)
        {
            if (lib.StartsWith("-l"))
                libs.Add(lib[2..]);
        }

        LibraryPaths = libPaths.ToArray();
        Libraries = libs.ToArray();
        IncludePaths = includePaths.ToArray();
        Defines = defines;
    }
}