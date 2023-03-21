using Borz.Core.Languages.C;
using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpModule]
public class BorzModule
{
    [MoonSharpModuleMethod]
    public static DynValue doborz(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        Script s = executionContext.GetScript();
        DynValue v = arguments.AsType(0, "doborz", DataType.String, false);

        var oldCwd = s.GetCwd();

        string fullPath = Path.GetFullPath(v.String, oldCwd);

        var attribs = File.GetAttributes(fullPath);
        if (attribs.HasFlag(FileAttributes.Directory))
        {
            fullPath = Path.Combine(fullPath, "build.borz");
        }

        DynValue fn = s.LoadFile(fullPath);
        s.SetCwd(Path.GetDirectoryName(fullPath)!);

        var data = new TailCallData()
        {
            Function = fn,
            Continuation = new CallbackFunction((c, a) =>
            {
                c.GetScript().SetCwd(oldCwd);
                return null;
            }),
            Args = new DynValue[] { }
        };

        Workspace.ExecutedBorzFiles.Add(fullPath);
        return DynValue.NewTailCallReq(data);
    }

    //Example: project("Test", Language.Cpp, BinType.Executable)
    [MoonSharpModuleMethod]
    public static DynValue project(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        Script s = executionContext.GetScript();
        DynValue nameV = arguments.AsType(0, "project", DataType.String, false);
        DynValue languageV = arguments.AsType(1, "project", DataType.UserData, false);
        DynValue typeV = arguments.AsType(2, "project", DataType.UserData, false);

        string name = nameV.String;
        Language language = (Language)languageV.UserData.Object;
        BinType type = (BinType)typeV.UserData.Object;

        var project = Project.Create(s, name, type, language);
        return DynValue.FromObject(s, project);
    }

    //Example: createPkgDep(ARRAY_LIBRARY_PATHS, ARRAY_LIBRARY_NAMES, ARRAY_INCLUDE_PATHS, TABLE_DEFINES, NEEDSRPATH)
    [MoonSharpModuleMethod]
    public static DynValue createPkgDep(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        Script s = executionContext.GetScript();
        DynValue libraryPathsV = arguments.AsType(0, "createPkgDep", DataType.Table, false);
        DynValue libraryNamesV = arguments.AsType(1, "createPkgDep", DataType.Table, false);
        DynValue includePathsV = arguments.AsType(2, "createPkgDep", DataType.Table, false);
        DynValue definesV = arguments.AsType(3, "createPkgDep", DataType.Table, false);
        DynValue needsRPathV = arguments.AsType(4, "createPkgDep", DataType.Boolean, false);

        var libraryPaths = new List<string>();
        var libraryNames = new List<string>();
        var includePaths = new List<string>();
        var defines = new Dictionary<string, string?>();

        foreach (var kv in libraryPathsV.Table.Pairs)
            libraryPaths.Add(kv.Value.String);

        foreach (var kv in libraryNamesV.Table.Pairs)
            libraryNames.Add(kv.Value.String);

        foreach (var kv in includePathsV.Table.Pairs)
            includePaths.Add(kv.Value.String);

        if (!definesV.IsNil())
            foreach (var kv in definesV.Table.Pairs)
                defines.Add(kv.Key.String, kv.Value.IsNil() ? null : kv.Value.String);

        var pkgDep = new PkgDep(libraryNames.ToArray(), libraryPaths.ToArray(), defines, includePaths.ToArray(),
            needsRPathV.Boolean);
        return DynValue.FromObject(s, pkgDep);
    }
}