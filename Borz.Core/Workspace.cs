using AkoSharp;
using Borz.Core.Lua;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace Borz.Core;

public static class Workspace
{
    public static string Location = String.Empty;
    public static List<Project> Projects = new();
    public static List<string> ExecutedBorzFiles = new();

    public static WorkspaceSettings Settings = new();
    public static Script Script;

    public static void CallPreCompileEvent()
    {
        var preCompileCallback = Workspace.Script.Globals["OnPreCompile"];
        if (preCompileCallback is Closure pcd)
        {
            pcd.Call();
        }
    }

    public static void CallPostCompileEvent()
    {
        var postCompileCallback = Workspace.Script.Globals["OnPostCompile"];
        if (postCompileCallback is Closure pcd)
        {
            pcd.Call();
        }
    }

    //Does init for workspace and running the inital borz script in current directory
    public static void Init(string location)
    {
        Project.Setup();
        ((ScriptLoaderBase)Script.DefaultOptions.ScriptLoader).ModulePaths = new string[] { "./?", "./?.lua" };
        ScriptRunner.RegisterTypes();

        Location = Path.GetFullPath(location);

        var projectConfig = Path.Combine(Location, "borzsettings.ako");
        if (File.Exists(projectConfig))
            Deserializer.FromString(Borz.Config.GetLayer(ConfLevel.Workspace),
                File.ReadAllText(projectConfig));

        var userProjectConfig = Path.Combine(Workspace.Location, ".borz", "usersettings.ako");

        Run();
    }

    public static void Run()
    {
        //Assume if no target platform is set, we are building for host platform
        if (Borz.BuildConfig.TargetPlatform == Lua.Platform.Unknown)
            Borz.BuildConfig.TargetPlatform = Borz.BuildConfig.HostPlatform;

        Script = ScriptRunner.CreateScript();
        Script.SetCwd(Location);
        ExecutedBorzFiles.Add(Path.GetFullPath("build.borz"));
        try
        {
            Script.DoFile("build.borz");
        }
        catch (Exception exception)
        {
            if (exception is InterpreterException runtimeError)
            {
                MugiLog.Fatal(runtimeError.DecoratedMessage);
            }
            else
                MugiLog.Fatal(exception.Message);

            MugiLog.Wait();
            MugiLog.Shutdown();
            //rethrow
            throw;
        }
    }

    public static void Reset()
    {
        Workspace.Projects.Clear();
        Workspace.ExecutedBorzFiles.Clear();
    }
}