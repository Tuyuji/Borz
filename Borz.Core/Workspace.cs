using AkoSharp;

namespace Borz.Core;

public static class Workspace
{
    public static string Location = String.Empty;
    public static List<Project> Projects = new();
    public static List<string> ExecutedBorzFiles = new();

    public static WorkspaceSettings Settings = new();

    //Does init for workspace and running the inital borz script in current directory
    public static void Init(string location)
    {
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
        if (Borz.BuildConfig.TargetPlatform == Lua.Platform.Unknown ||
            string.IsNullOrEmpty(Borz.BuildConfig.TargetPlatform))
            Borz.BuildConfig.TargetPlatform = Borz.BuildConfig.HostPlatform;

        var borzFile = Utils.GetBorzScriptFilePath(Location);
        if (borzFile == null)
        {
            MugiLog.Error($"No borz script found in {Location}, need build.borz or borz.lua in root directory");
            return;
        }

        ExecutedBorzFiles.Add(borzFile);
        Borz.RunScript(borzFile);
    }

    public static void Reset()
    {
        Workspace.Projects.Clear();
        Workspace.ExecutedBorzFiles.Clear();
    }
}