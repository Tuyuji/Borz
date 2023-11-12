using AkoSharp;
using QuickGraph;
using QuickGraph.Algorithms.TopologicalSort;

namespace Borz.Core;

[BorzUserData]
public static class Workspace
{
    public static string Location = string.Empty;
    public static List<Project> Projects = new();
    public static List<string> ExecutedBorzFiles = new();

    public static string Name = "Workspace";
    public static List<string> Configs = new();

    static Workspace()
    {
        Configs.Add("debug");
        Configs.Add("release");
    }

    //Does init for workspace and running the inital borz script in current directory
    public static void Init(string location)
    {
        Location = Path.GetFullPath(location);

        var projectConfig = Path.Combine(Location, "borzsettings.ako");
        if (File.Exists(projectConfig))
            Deserializer.FromString(Borz.Config.GetLayer(ConfLevel.Workspace),
                File.ReadAllText(projectConfig));

        var userProjectConfig = Path.Combine(Location, ".borz", "usersettings.ako");

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
        Projects.Clear();
        ExecutedBorzFiles.Clear();
    }

    public static List<Project>? GetSortedProjectList()
    {
        var graph = new AdjacencyGraph<Project, Edge<Project>>();
        graph.AddVertexRange(Projects);
        foreach (var project in Projects)
        foreach (var dependency in project.Dependencies)
            graph.AddEdge(new Edge<Project>(dependency, project));

        var algorithm = new TopologicalSortAlgorithm<Project, Edge<Project>>(graph);
        try
        {
            algorithm.Compute();
        }
        catch (NonAcyclicGraphException e)
        {
            MugiLog.Fatal("Cyclic/Circular dependency detected, cannot continue.");
            return null;
        }

        return algorithm.SortedVertices.ToList();
    }
}