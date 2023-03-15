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
        script.Globals["Util"] = typeof(Util);
        script.Globals["Log"] = typeof(Log);
        script.Globals["PkgConfig"] = typeof(LuaPkgConf);


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
}