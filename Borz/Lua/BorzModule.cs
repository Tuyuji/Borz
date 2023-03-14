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
            Args = new DynValue[]{}
        };
        
        Workspace.ExecutedBorzFiles.Add(fullPath);
        return DynValue.NewTailCallReq(data);
    }

}