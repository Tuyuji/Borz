using Borz.Lua;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using MoonSharp.Interpreter;

namespace Borz.Languages.C;

[MoonSharpUserData]
[ProjectLanguage(Language.C)]
public class CProject : Project
{
    public Dictionary<string, string?> Defines = new();
    public List<string> SourceFiles = new();
    public List<string> PublicIncludePaths = new();
    public List<string> PrivateIncludePaths = new();
    public List<string> LibraryPaths = new();
    public List<string> Links = new();
    public bool UsePIC = false;
    public string StdVersion = "";

    public CProject(string name, BinType type, string directory = "", Language language = Language.C) : base(name, type,
        language, directory)
    {
    }

    public static CProject Create(Script script, string name, BinType type)
    {
        var proj = new CProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }

    public void AddDefine(string name, string? value = null)
    {
        if (Defines.ContainsKey(name))
            Defines[name] = value;
        else
            Defines.Add(name, value);
    }


    public void AddLibraryPath(string path)
    {
        if (LibraryPaths.Contains(path))
            return;

        LibraryPaths.Add(path);
    }

    public void AddIncludePath(string path, bool isPrivate = true)
    {
        if (isPrivate)
        {
            if (PrivateIncludePaths.Contains(path))
                return;

            PrivateIncludePaths.Add(path);
        }
        else
        {
            if (PublicIncludePaths.Contains(path))
                return;

            PublicIncludePaths.Add(path);
        }
    }

    public void AddIncludePaths(string[] paths, bool isPrivate = true)
    {
        foreach (string s in paths)
        {
            AddIncludePath(s, isPrivate);
        }
    }

    public void AddLink(string name)
    {
        if (Links.Contains(name))
            return;

        Links.Add(name);
    }

    public void AddLinks(string[] names)
    {
        foreach (string s in names)
        {
            AddLink(s);
        }
    }

    public void AddSourceGlob(string match)
    {
        Matcher matcher = new Matcher();
        matcher.AddInclude(match);

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(ProjectDirectory)));
        if (!result.HasMatches) return;
        foreach (var file in result.Files)
        {
            SourceFiles.Add(file.Path);
        }
    }

    public void AddSourceFile(string path)
    {
        if (SourceFiles.Contains(path))
            return;

        SourceFiles.Add(path);
    }

    public void AddSourceFiles(string[] paths)
    {
        foreach (string s in paths)
        {
            AddSourceFile(s);
        }
    }
}