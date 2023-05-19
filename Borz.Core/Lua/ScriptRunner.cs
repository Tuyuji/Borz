using System.Reflection;
using Borz.Core.PkgConfig;
using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

public static class ScriptRunner
{
    public static void RegisterTypes()
    {
        //Get all classes with MoonSharpUserData attribute and register them
        var userDataTypes = Assembly.GetExecutingAssembly().GetTypes().Where(e =>
            e.GetCustomAttribute<MoonSharpUserDataAttribute>() != null ||
            e.GetCustomAttribute<BorzUserDataAttribute>() != null
        ).ToArray();
        foreach (Type type in userDataTypes)
        {
            UserData.RegisterType(type);
        }

        UserData.RegisterType<Guid>();
        UserData.RegisterType<EventArgs>();
    }

    public static Script CreateScript()
    {
        var script = new Script();

        script.Globals.RegisterModuleType<BorzModule>();
        script.Globals["Project"] = typeof(Project);
        script.Globals["util"] = typeof(Util);
        script.Globals["dir"] = typeof(LuaDir);
        script.Globals["file"] = typeof(LuaFile);
        script.Globals["path"] = typeof(LuaPath);
        script.Globals["log"] = typeof(Log);
        script.Globals["pkgconf"] = typeof(LuaPkgConf);
        script.Globals["BuildConf"] = Borz.BuildConfig;
        script.Globals["ws"] = Workspace.Settings;


        var types = typeof(Project).Assembly.GetTypes();
        foreach (var type in types)
        {
            if (type.IsSubclassOf(typeof(Project)))
            {
                script.Globals[type.Name] = type;
            }
        }

        //Enums
        script.Globals["BinType"] = typeof(BinType);
        script.Globals["Language"] = typeof(Language);
        script.Globals["ResourceType"] = typeof(Util.ResourceType);
        script.Globals["Platform"] = typeof(Platform);
        script.Globals["VersionType"] = typeof(VersionType);

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
}