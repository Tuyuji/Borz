using Borz.Core.Lua;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using MoonSharp.Interpreter;

namespace Borz.Core.Languages.C;

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
    public Dictionary<PkgDep, bool> PkgDeps = new();
    public bool UsePIC = false;
    public bool Symbols = false;
    public string PchSource = "";
    public string PchHeader = "";

    //Version number for the C or Cpp standard to use.
    public string StdVersion;

    //This is used to determine if the project was built or not.
    //If this is set to true, then the output binrary was created or updated.
    public bool IsBuilt = false;

    public CProject(string name, BinType type, string directory = "", Language language = Language.C) : base(name, type,
        language, directory)
    {
        StdVersion = "11";
    }

    public static CProject Create(Script script, string name, BinType type)
    {
        var proj = new CProject(name, type, script.GetCwd());
        script.Globals.Set(name, DynValue.FromObject(script, proj));
        return proj;
    }

    public void AddDep(PkgDep dep, bool isPublic = false)
    {
        if (PkgDeps.ContainsKey(dep))
            return;
        PkgDeps.Add(dep, isPublic);
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

    public void AddIncludePath(string path, bool isPublic = true)
    {
        if (isPublic)
        {
            if (PublicIncludePaths.Contains(path))
                return;

            PublicIncludePaths.Add(path);
        }
        else
        {
            if (PrivateIncludePaths.Contains(path))
                return;

            PrivateIncludePaths.Add(path);
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

    #region Compiler Api

    //This is for Compilers or Generators to use
    //It will return all the include paths for this project 
    public string[] GetIncludePaths()
    {
        List<string> paths = new();
        paths.AddRange(PublicIncludePaths);
        paths.AddRange(PrivateIncludePaths);
        foreach (var dep in PkgDeps)
        {
            paths.AddRange(dep.Key.Includes);
        }

        foreach (var dependency in Dependencies)
        {
            if (dependency is CppProject cppProject)
            {
                paths.AddRange(cppProject.GetPublicIncludePaths());
            }
            else if (dependency is CProject cProject)
            {
                paths.AddRange(cProject.GetPublicIncludePaths());
            }
        }

        return paths.ToArray();
    }

    public string[] GetPublicIncludePaths()
    {
        List<string> paths = new();
        paths.AddRange(GetPathsAbs(PublicIncludePaths.ToArray()));

        //Already absolute
        foreach (var dep in PkgDeps.Where(dep => dep.Value))
        {
            paths.AddRange(dep.Key.Includes);
        }

        foreach (var dependency in Dependencies)
        {
            if (dependency is CppProject cppProject)
            {
                paths.AddRange(cppProject.GetPublicIncludePaths());
            }
            else if (dependency is CProject cProject)
            {
                paths.AddRange(cProject.GetPublicIncludePaths());
            }
        }

        return paths.ToArray();
    }

    public string[] GetLibraryPaths()
    {
        List<string> paths = new();
        //make sure its absolute
        paths.AddRange(
            LibraryPaths.Select(e => Path.IsPathRooted(e) ? e : Path.Combine(ProjectDirectory, e)));

        //PkgDeps are absolute
        foreach (var dep in PkgDeps)
        {
            paths.AddRange(dep.Key.LibDirs);
        }

        foreach (var dependency in Dependencies)
        {
            switch (dependency)
            {
                case CppProject cppProject:
                    paths.Add(cppProject.GetPathAbs(cppProject.OutputDirectory));
                    break;
                case CProject cProject:
                    paths.Add(cProject.GetPathAbs(cProject.OutputDirectory));
                    break;
            }
        }

        return paths.ToArray();
    }

    public string[] GetRPaths(string outputDir)
    {
        //needs to be relative to the output dir
        List<string> paths = new();
        foreach (var dep in PkgDeps)
        {
            if (dep.Key.RequiresRpath)
                paths.AddRange(dep.Key.LibDirs.Select(e => Path.GetRelativePath(outputDir, e)));
        }

        foreach (var dependency in Dependencies)
        {
            switch (dependency)
            {
                case CppProject cppProject:
                    paths.Add(Path.GetRelativePath(outputDir, cppProject.GetPathAbs(cppProject.OutputDirectory)));
                    break;
                case CProject cProject:
                    paths.Add(Path.GetRelativePath(outputDir, cProject.GetPathAbs(cProject.OutputDirectory)));
                    break;
            }
        }

        return paths.ToArray();
    }

    public string[] GetLibraries()
    {
        List<string> libs = new();
        libs.AddRange(Links);
        foreach (var dep in PkgDeps)
        {
            libs.AddRange(dep.Key.Libs);
        }

        foreach (var dependency in Dependencies)
        {
            switch (dependency)
            {
                case CppProject cppProject:
                    libs.Add(cppProject.Name);
                    break;
                case CProject cProject:
                    libs.Add(cProject.Name);
                    break;
            }
        }

        return libs.ToArray();
    }

    public IDictionary<string, string?> GetDefines()
    {
        Dictionary<string, string?> defs =
            Defines.ToDictionary(valuePair => valuePair.Key, valuePair => valuePair.Value);

        foreach (var valuePair in PkgDeps.SelectMany(dep => dep.Key.Defines))
        {
            defs.Add(valuePair.Key, valuePair.Value);
        }

        return defs;
    }

    #endregion
}