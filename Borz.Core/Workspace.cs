using AkoSharp;
using MoonSharp.Interpreter;
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
    public static BuildConfig BuildCfg = new();

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

    public static void Configs(Script script, DynValue def)
    {
        BuildCfg.ValidConfigs.Clear();
        if (def.Type == DataType.Table)
            foreach (var v in def.Table.Values)
                BuildCfg.ValidConfigs.Add(v.String);
        else
            BuildCfg.ValidConfigs.Add(def.String);
    }

    public static void Run()
    {
        //Assume if no target platform is set, we are building for host platform
        if (BuildCfg.TargetPlatform == Lua.Platform.Unknown ||
            string.IsNullOrEmpty(BuildCfg.TargetPlatform))
            BuildCfg.TargetPlatform = BuildCfg.HostPlatform;

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

    public static bool Compile(bool simulate = false)
    {
        Borz.UpdateMemInfo();

        if (BuildCfg.TargetPlatform == Lua.Platform.Wasm)
        {
            var sdkDir = Borz.Config.Get("webasm", "sdk");
            if (sdkDir == null)
            {
                MugiLog.Fatal("WebAssembly SDK directory not set in config.");
                return false;
            }

            var sdkPath = Path.GetFullPath(sdkDir);
            if (!Directory.Exists(sdkPath))
            {
                MugiLog.Fatal("WebAssembly SDK directory does not exist.");
                return false;
            }

            var upstreamPath = Path.Combine(sdkPath, "upstream", "emscripten");

            var envPathSep = ':';

            //add sdk path to PATH
            var path = Environment.GetEnvironmentVariable("PATH");
            path += envPathSep + sdkPath + envPathSep + upstreamPath;
            Environment.SetEnvironmentVariable("PATH", path);
        }

        var sortedProjects = GetSortedProjectList();
        if (sortedProjects == null)
        {
            MugiLog.Fatal("Cyclic/Circular dependency detected, cannot continue.");
            return false;
        }

        MugiLog.Info($"Config: {BuildCfg}");

        sortedProjects.ForEach(prj =>
        {
            MugiLog.Info("===========================================");
            var builder = BuildFactory.GetBuilder(prj.Language);
            builder.Build(prj, simulate);
        });

        return true;
    }

    public static bool Clean(bool justLog = false)
    {
        void Delete(string path, bool recursive)
        {
            if (justLog)
            {
                MugiLog.Info($"Deleting {path}...");
                return;
            }

            Directory.Delete(path, recursive);
        }

        Projects.ForEach(prj =>
        {
            var intDir = prj.GetPathAbs(prj.IntermediateDirectory);
            var outDir = prj.GetPathAbs(prj.OutputDirectory);
            if (Directory.Exists(intDir))
                Delete(intDir, true);
            if (Directory.Exists(outDir))
                Delete(outDir, true);
        });

        return true;
    }

    /// <summary>
    /// Trys to find the generator with the given name.
    /// </summary>
    /// <param name="generator">Name</param>
    /// <returns>Returns false if not found.</returns>
    public static bool Generate(string generator)
    {
        if (Borz.Generators.ContainsKey(generator))
        {
            Borz.Generators[generator].Invoke();
            return true;
        }

        return false;
    }
}