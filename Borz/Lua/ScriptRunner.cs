using System.Reflection;
using Borz.PkgConfig;
using MoonSharp.Interpreter;

namespace Borz.Lua;

public static class ScriptRunner
{
    public static void RegisterTypes()
    {
        //Get all classes with MoonSharpUserData attribute and register them
        var userDataTypes = Assembly.GetExecutingAssembly().GetTypes().Where(e =>
            e.GetCustomAttribute<MoonSharpUserDataAttribute>() != null
        ).ToArray();
        foreach (var type in userDataTypes) UserData.RegisterType(type);

        UserData.RegisterType<Guid>();
        UserData.RegisterType<EventArgs>();
        UserData.RegisterType<BinType>();
        UserData.RegisterType<Endianness>();
        UserData.RegisterType<VersionType>();
        UserData.RegisterType<Workspace.ProjectAddedEvent>();
    }

    public static Script CreateScript()
    {
        var script = new Script();

        script.Globals.RegisterModuleType<BorzModule>();
        script.Globals["Project"] = typeof(Project);
        script.Globals["MachineInfo"] = typeof(MachineInfo);
        script.Globals["util"] = typeof(Utils);
        script.Globals["dir"] = typeof(LuaDir);
        script.Globals["file"] = typeof(LuaFile);
        script.Globals["path"] = typeof(LuaPath);
        script.Globals["log"] = typeof(Log);
        script.Globals["pkgconf"] = typeof(LuaPkgConf);

        //Enums
        script.Globals["BinType"] = typeof(BinType);
        script.Globals["Lang"] = typeof(Lang);
        script.Globals["Language"] = typeof(Lang);
        script.Globals["ResourceType"] = typeof(Utils.ResourceType);
        // script.Globals["Platform"] = typeof(Platform);
        // script.Globals["VersionType"] = typeof(VersionType);
        // script.Globals["PhobosType"] = typeof(PhobosType);

        return script;
    }

    public static void SetCwd(this Script script, string newCwd)
    {
        script.Globals["cwd"] = newCwd;
    }

    public static string GetCwd(this Script script)
    {
        return script.Globals.Get("cwd").String;
    }

    public static string GetAbsolute(this Script script, string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(script.GetCwd(), path);
        return path;
    }

    public static void CallPreCompileEvent(this Script script)
    {
        var preCompileCallback = script.Globals["OnPreCompile"];
        if (preCompileCallback is Closure pcd) pcd.Call();
    }

    public static void CallPostCompileEvent(this Script script)
    {
        var postCompileCallback = script.Globals["OnPostCompile"];
        if (postCompileCallback is Closure pcd) pcd.Call();
    }

    public static void Eval(Script script, string scriptPath)
    {
        var fullPath = Path.GetFullPath(scriptPath);
        var dir = Path.GetDirectoryName(fullPath);

        script.SetCwd(dir!);
        try
        {
            script.DoFile(fullPath);
        }
        catch (Exception exception)
        {
            if (exception is InterpreterException runtimeError)
                MugiLog.Fatal(runtimeError.DecoratedMessage);
            else
                MugiLog.Fatal(exception.Message);

            MugiLog.Wait();
            MugiLog.Shutdown();
            //rethrow
            throw;
        }
    }
}