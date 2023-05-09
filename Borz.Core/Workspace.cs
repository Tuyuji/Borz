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

    //Does init for workspace and running the inital borz script in current directory
    public static void Init()
    {
        Project.Setup();
        ((ScriptLoaderBase)Script.DefaultOptions.ScriptLoader).ModulePaths = new string[] { "./?", "./?.lua" };
        ScriptRunner.RegisterTypes();

        Location = Directory.GetCurrentDirectory();

        var projectConfig = Path.Combine(Location, "borzsettings.ako");
        if (File.Exists(projectConfig))
            Deserializer.FromString(Borz.Config.GetLayer(ConfLevel.Workspace),
                File.ReadAllText(projectConfig));

        var userProjectConfig = Path.Combine(Workspace.Location, ".borz", "usersettings.ako");

        Run();
    }

    public static void Run()
    {
        var script = ScriptRunner.CreateScript();
        script.SetCwd(Directory.GetCurrentDirectory());
        ExecutedBorzFiles.Add(Path.GetFullPath("build.borz"));
        try
        {
            script.DoFile("build.borz");
        }
        catch (Exception exception)
        {
            if (exception is SyntaxErrorException syntaxError)
                MugiLog.Fatal($"File {syntaxError.Source}  Error: " + syntaxError.Message);
            else if (exception is ScriptRuntimeException runtimeError)
            {
                var callstack = runtimeError.CallStack[0];
                if (callstack != null)
                {
                    var line = callstack.Location.FromLine;
                    MugiLog.Fatal($"Line {line} Error: " + runtimeError.Message);
                }
                else
                {
                    MugiLog.Fatal(runtimeError.Message);
                }
            }

            Environment.Exit(1);
        }
    }

    public static void Reset()
    {
        Workspace.Projects.Clear();
        Workspace.ExecutedBorzFiles.Clear();
    }
}