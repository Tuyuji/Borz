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
    //If set to none, then no standard will be used.
    public string StdVersion;

    //This is used to determine if the project was built or not.
    //If this is set to true, then the output binrary was created or updated.
    [MoonSharpHidden] public bool IsBuilt = false;

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

    #region Lua specific

    #region Lua Include

    private void LuaInclude(DynValue path, bool isPublic = true)
    {
        if (path.Type == DataType.String)
        {
            AddIncludePath(path.String, isPublic);
            return;
        }

        if (path.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected string or table for path");

        foreach (var v in path.Table.Values)
        {
            if (v.Type != DataType.String)
                throw new ScriptRuntimeException("Expected string elements in table");

            AddIncludePath(v.String, isPublic);
        }
    }

    public void pubInclude(Script script, DynValue path)
    {
        LuaInclude(path, true);
    }

    public void privInclude(Script script, DynValue path)
    {
        LuaInclude(path, false);
    }

    #endregion

    #region Lua Source

    public void source(Script script, DynValue path)
    {
        if (path.Type == DataType.String)
        {
            AddSourceFile(path.String);
            return;
        }

        if (path.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected string or table for path");

        foreach (var v in path.Table.Values)
        {
            if (v.Type != DataType.String)
                throw new ScriptRuntimeException("Expected string elements in table");

            AddSourceFile(v.String);
        }
    }

    public void sourceGlob(Script script, DynValue path)
    {
        if (path.Type == DataType.String)
        {
            AddSourceGlob(path.String);
            return;
        }

        if (path.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected string or table for path");

        foreach (var v in path.Table.Values)
        {
            if (v.Type != DataType.String)
                throw new ScriptRuntimeException("Expected string elements in table");

            AddSourceFile(v.String);
        }
    }

    #endregion

    #region Lua Dep

    private void LuaAddDep(DynValue dep, bool isPublic = false)
    {
        if (dep.Type != DataType.UserData)
            throw new ScriptRuntimeException("Expected PkgDep or Project for dep");

        if (dep.UserData.Object is PkgDep pkgDep)
        {
            AddDep(pkgDep, isPublic);
            return;
        }

        if (dep.UserData.Object is Project proj)
        {
            AddDep(proj);
            return;
        }
    }

    /*
     * prj.dep {
     *      {sdl, true},
     * }
     */
    public void dep(Script script, DynValue depTable)
    {
        if (depTable.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected table for dep");

        foreach (var v in depTable.Table.Values)
        {
            //should be a table with 2 elements
            //first element is the dep
            //second element is if it is public or not
            if (v.Type != DataType.Table)
                throw new ScriptRuntimeException("Expected table elements in table");

            if (v.Table.Length != 2)
                throw new ScriptRuntimeException("Expected table with 2 elements");

            var dep = v.Table.Get(0) ?? throw new ScriptRuntimeException("Expected dep in table");
            var isPublic = v.Table.Get(1) ?? throw new ScriptRuntimeException("Expected isPublic in table");

            if (dep.Type != DataType.UserData)
                throw new ScriptRuntimeException("Expected PkgDep or Project for dep");

            if (isPublic.Type != DataType.Boolean)
                throw new ScriptRuntimeException("Expected boolean for isPublic");


            LuaAddDep(dep, isPublic.Boolean);
        }
    }

    /*
     * prj.pubDep {
     *      sdl,
     * }
     */
    public void pubDep(Script script, DynValue dep)
    {
        if (dep.Type == DataType.Table)
        {
            foreach (var v in dep.Table.Values)
            {
                LuaAddDep(v, true);
            }
        }
        else
        {
            LuaAddDep(dep, true);
        }
    }

    public void privDep(Script script, DynValue dep)
    {
        if (dep.Type == DataType.Table)
        {
            foreach (var v in dep.Table.Values)
            {
                LuaAddDep(v, false);
            }
        }
        else
        {
            LuaAddDep(dep, false);
        }
    }

    #endregion

    #region Lua Define

    private void LuaAddDefine(DynValue define)
    {
        if (define.Type == DataType.String)
        {
            var str = define.String;
            if (str == null)
                throw new ScriptRuntimeException("Expected string for define");

            if (!str.Contains('='))
            {
                AddDefine(str);
                return;
            }

            var split = str.Split('=', 2);
            if (split.Length != 2)
                throw new ScriptRuntimeException("Expected string in format of 'name=value' for define");

            AddDefine(split[0], split[1]);
            return;
        }
    }

    /*
     * prj.define "ASDF"
     * prj.define "ASDF=YES"
     * prj.define {"ASDF", "ASDF=YES"}
     */
    public void define(Script script, DynValue def)
    {
        if (def.Type == DataType.Table)
        {
            foreach (var v in def.Table.Values)
            {
                LuaAddDefine(v);
            }
        }
        else
        {
            LuaAddDefine(def);
        }
    }

    #endregion

    #region Lua LibraryPath

    public void libPath(Script script, DynValue path)
    {
        if (path.Type == DataType.String)
        {
            AddLibraryPath(path.String);
            return;
        }

        if (path.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected string or table for path");

        foreach (var v in path.Table.Values)
        {
            if (v.Type != DataType.String)
                throw new ScriptRuntimeException("Expected string elements in table");

            AddLibraryPath(v.String);
        }
    }

    #endregion

    #region Lua Links

    public void links(Script script, DynValue libs)
    {
        if (libs.Type == DataType.String)
        {
            AddLink(libs.String);
            return;
        }

        if (libs.Type != DataType.Table)
            throw new ScriptRuntimeException("Expected string or table for libs");

        foreach (var v in libs.Table.Values)
        {
            if (v.Type != DataType.String)
                throw new ScriptRuntimeException("Expected string elements in table");

            AddLink(v.String);
        }
    }

    #endregion

    #endregion

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

    public void AddIncludePaths(string[] paths, bool isPublic = true)
    {
        foreach (string s in paths)
        {
            AddIncludePath(s, isPublic);
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