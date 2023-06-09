using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using AkoSharp;
using Borz.Core.Platform;
using ByteSizeLib;
using QuickGraph;
using QuickGraph.Algorithms.TopologicalSort;

namespace Borz.Core;

public static class Borz
{
    public static ConfigLayers<ConfLevel> Config = new();

    public static ParallelOptions ParallelOptions = new();

    public static BuildConfig BuildConfig = new();

    public static ConcurrentQueue<string> BuildLog = new();

    public static void Init()
    {
        MugiLog.Init();

        ShortTypeRegistry.AutoRegister();

        //Load up platform assembly
        LoadPlatformAssembly();

        var configFolder = Path.Combine(IPlatform.Instance.GetUserConfigPath(), "borz");
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        var configFile = Path.Combine(configFolder, "config.ako");

        //Load defaults into config
        SetupDefaults();

        if (File.Exists(configFile))
        {
            Deserializer.FromString(Config.GetLayer(ConfLevel.UserGobal), File.ReadAllText(configFile));
        }
        else
        {
            File.WriteAllText(configFile, "# Borz Configuration File\ntemp;");
        }

        ConfigChanged();
    }

    private static void LoadPlatformAssembly()
    {
        var dllName = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            dllName = "Borz.Linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            dllName = "Borz.MacOS";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dllName = "Borz.Windows";
        }
        else
        {
            throw new Exception("Platform not supported");
        }

        var dllFilename = $"{dllName}.dll";
        //Get the executables path
        var path = Path.GetDirectoryName(Environment.ProcessPath);
        if (path == null)
            throw new Exception("Could not get path to executable.");

        var fullDllPath = Path.Combine(path, dllFilename);

        if (!File.Exists(fullDllPath))
            throw new Exception("Platform assembly not found.");

        var platAssembly = Assembly.LoadFrom(fullDllPath);
        ShortTypeRegistry.Register(platAssembly);

        //Now get embedded resource called platform.ako and load it into the config
        var resourceName = $"{dllName}.platform.ako";
        using (Stream stream = platAssembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            string result = reader.ReadToEnd();
            Deserializer.FromString(Config.GetLayer(ConfLevel.Platform), result);
        }
    }

    public static void Shutdown()
    {
        MugiLog.Shutdown();
        //write build log to /tmp/borz-build.log
        //if a build log already exists, append to it
        var buildLogPath = Path.Combine(Path.GetTempPath(), "borz-build.log");
        if (File.Exists(buildLogPath))
        {
            BuildLog.Enqueue($"=======================");
            BuildLog.Enqueue($"=======================");
            BuildLog.Enqueue($"{DateTime.Now:g}");
            BuildLog.Enqueue($"=======================");
            BuildLog.Enqueue($"=======================");
            File.AppendAllLines(buildLogPath, BuildLog);
        }
        else
        {
            File.WriteAllLines(buildLogPath, BuildLog);
        }
    }

    public static bool UseMold
    {
        get
        {
            bool use = false;
            var conf = Config.Get("linker", "mold");
            if (conf == true)
                use = true;
            return use;
        }
    }

    private static void SetupDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Borz.Core.defaults.ako";
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            string result = reader.ReadToEnd();
            Deserializer.FromString(Config.GetLayer(ConfLevel.Defaults), result);
        }
    }

    public static void ConfigChanged()
    {
        {
            var levelStr = Config.Get("log", "level");
            LogLevel level = LogLevel.Info;
            if (!Enum.TryParse(levelStr, true, out level))
            {
                MugiLog.Error("Failed to parse log level from config, defaulting to Info.");
            }

            MugiLog.MinLevel = level;
        }
    }

    public static void UpdateMemInfo()
    {
        //String in format like "1G" or "2M" or "3K"
        var perThreadMinMemory = (string?)Config.Get("mt", "minThreadMem");
        var perThreadMinMemoryGB = ByteSize.Parse(perThreadMinMemory).GigaBytes;

        var maxCpuCount = Environment.ProcessorCount;

        var maxReqThreads = (int)Config.Get("mt", "maxThreads");
        if (maxReqThreads == -1 || maxReqThreads == 0)
        {
            //Use cpu max
            maxReqThreads = maxCpuCount;
        }

        if (maxReqThreads > maxCpuCount)
        {
            maxReqThreads = maxCpuCount;
            MugiLog.Warning($"Max threads requested is greater than the number of CPUs, capping at {maxCpuCount}.");
        }

        var memoryInfo = IPlatform.Instance.GetMemoryInfo();
        var totalMemory = memoryInfo.Total;
        var availableMemory = memoryInfo.Available;

        var totalMemoryGB = totalMemory.GigaBytes;
        var availableMemoryGB = availableMemory.GigaBytes;

        var usableThreadCount = Convert.ToInt32(Math.Floor(availableMemoryGB / perThreadMinMemoryGB));
        if (usableThreadCount > maxCpuCount)
            usableThreadCount = maxCpuCount;

        MugiLog.Debug("Total Memory: " + totalMemory);
        MugiLog.Debug("Available Memory: " + availableMemory);
        MugiLog.Debug("Per Thread Min Memory: " + perThreadMinMemory);
        MugiLog.Debug("Max Threads: " + maxReqThreads);

        MugiLog.Debug($"Using {usableThreadCount} threads, " +
                      $"({availableMemoryGB:F2}GB/{perThreadMinMemoryGB:F2}GB = " +
                      $"{availableMemoryGB / perThreadMinMemoryGB:F2})");


        ParallelOptions.MaxDegreeOfParallelism = usableThreadCount;
    }

    public static void RunScript(string location)
    {
        Workspace.Init(location);
    }

    public static bool CompileWorkspace(bool justLog = false)
    {
        Core.Borz.UpdateMemInfo();

        var graph = new AdjacencyGraph<Project, Edge<Project>>();
        graph.AddVertexRange(Workspace.Projects);
        foreach (var project in Workspace.Projects)
        {
            foreach (var dependency in project.Dependencies)
            {
                graph.AddEdge(new Edge<Project>(dependency, project));
            }
        }

        var algorithm = new TopologicalSortAlgorithm<Project, Edge<Project>>(graph);
        try
        {
            algorithm.Compute();
        }
        catch (NonAcyclicGraphException e)
        {
            MugiLog.Fatal("Cyclic/Circular dependency detected, cannot continue.");
            return false;
        }

        var sortedProjects = algorithm.SortedVertices.ToList();

        MugiLog.Info($"Config: {BuildConfig.Config}");

        Workspace.CallPreCompileEvent();

        sortedProjects.ForEach(prj =>
        {
            MugiLog.Info("===========================================");
            var builder = IBuilder.GetBuilder(prj);
            builder.Build(prj, justLog);
        });

        Workspace.CallPostCompileEvent();

        return true;
    }

    public static bool CleanWorkspace()
    {
        Workspace.Projects.ForEach(prj =>
        {
            var intDir = prj.GetPathAbs(prj.IntermediateDirectory);
            var outDir = prj.GetPathAbs(prj.OutputDirectory);
            if (Directory.Exists(intDir))
                Directory.Delete(intDir, true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);
        });

        return true;
    }

    public static void GenerateWorkspace(IGenerator generator)
    {
        generator.Generate();
    }
}