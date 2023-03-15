using MoonSharp.Interpreter;

namespace Borz.Lua;

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
}