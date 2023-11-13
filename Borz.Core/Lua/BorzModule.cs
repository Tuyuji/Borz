using Borz.Core.Languages.C;
using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpModule]
public class BorzModule
{
    [MoonSharpModuleMethod]
    public static DynValue doborz(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        var s = executionContext.GetScript();
        var v = arguments.AsType(0, "doborz", DataType.String, false);

        var oldCwd = s.GetCwd();

        var fullPath = Path.GetFullPath(v.String, oldCwd);

        var attribs = File.GetAttributes(fullPath);
        if (attribs.HasFlag(FileAttributes.Directory))
        {
            var buildBorz = Utils.GetBorzScriptFilePath(fullPath);
            if (buildBorz == null)
            {
                var mainLua = Path.Combine(fullPath, "main.lua");
                fullPath = mainLua;
                if (!File.Exists(mainLua))
                    throw new Exception($"Could not find files main.lua, build.borz, borz.lua in directory {fullPath}");
            }
            else
            {
                fullPath = buildBorz;
            }
        }


        var friendlyName = Path.GetFileName(fullPath);
        if (Workspace.ExecutedBorzFiles.Count != 0) friendlyName = Path.GetRelativePath(Workspace.Location, fullPath);


        var fn = s.LoadFile(fullPath, null, friendlyName);
        s.SetCwd(Path.GetDirectoryName(fullPath)!);

        var data = new TailCallData()
        {
            Function = fn,
            Continuation = new CallbackFunction((c, a) =>
            {
                c.GetScript().SetCwd(oldCwd);
                if (a.Count == 1)
                    return a[0];
                if (a.Count == 0)
                    return DynValue.Nil;

                return DynValue.NewTuple(a.GetArray());
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
        var s = executionContext.GetScript();
        var nameV = arguments.AsType(0, "project", DataType.String, false);
        var languageV = arguments.AsType(1, "project", DataType.String, false);
        var typeV = arguments.AsType(2, "project", DataType.UserData, false);
        var tagsV = arguments.AsType(3, "project", DataType.Table, true);

        var name = nameV.String;
        var language = languageV.String;
        var type = (BinType)typeV.UserData.Object;
        List<string> tags = null;
        if (tagsV.IsNotNil()) tags = tagsV.ToObject<List<string>>();

        var project = (Project)Project.Create(s, name, type, language);
        if (tags != null)
            project.Tags = tags;

        var projectCallback = s.Globals["OnProjectCreate"];
        if (projectCallback is Closure pcd) pcd.Call(project);

        return DynValue.FromObject(s, project);
    }

    //Example: createPkgDep(ARRAY_LIBRARY_PATHS, ARRAY_LIBRARY_NAMES, ARRAY_INCLUDE_PATHS, TABLE_DEFINES, NEEDSRPATH)
    [MoonSharpModuleMethod]
    public static DynValue createPkgDep(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        var s = executionContext.GetScript();
        var libraryPathsV = arguments.AsType(0, "createPkgDep", DataType.Table, false);
        var libraryNamesV = arguments.AsType(1, "createPkgDep", DataType.Table, false);
        var includePathsV = arguments.AsType(2, "createPkgDep", DataType.Table, false);
        var definesV = arguments.AsType(3, "createPkgDep", DataType.Table, false);
        var needsRPathV = arguments.AsType(4, "createPkgDep", DataType.Boolean, false);

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

    [MoonSharpModuleMethod]
    public static DynValue isBuildConf(ScriptExecutionContext executionContext, CallbackArguments arguments)
    {
        var s = executionContext.GetScript();
        var buildConfV = arguments.AsType(0, "isBuildConf", DataType.String, false);

        var buildConf = buildConfV.String;
        return DynValue.NewBoolean(Workspace.BuildCfg.ConfigEquals(buildConf));
    }
}