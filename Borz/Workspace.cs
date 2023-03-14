using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using AkoSharp;
using Borz.Lua;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using Spectre.Console;

namespace Borz;

public static class Workspace
{
    
    public static string Location = String.Empty;
    public static List<Project> Projects = new();
    public static List<string> ExecutedBorzFiles = new();

    //Does init for workspace and running the inital borz script in current directory
    public static void Init()
    {
        ((ScriptLoaderBase)Script.DefaultOptions.ScriptLoader).ModulePaths = new string[] { "./?", "./?.lua" };
        ScriptRunner.RegisterTypes();

        Location = Directory.GetCurrentDirectory();

        var projectConfig = Path.Combine(Location, "borzsettings.ako");
        if(File.Exists(projectConfig))
            Deserializer.FromString(Utils.Config.GetLayer(ConfigLayers.LayerType.Workspace), File.ReadAllText(projectConfig));
        
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
                    MugiLog.Fatal($"Error: " + runtimeError.Message);
                }
            }
            else
                MugiLog.Fatal($"Error: " + exception);

            Environment.Exit(1);
        }
    }

    public static void Reset()
    {
        Workspace.Projects.Clear();
        Workspace.ExecutedBorzFiles.Clear();
        
    }
}