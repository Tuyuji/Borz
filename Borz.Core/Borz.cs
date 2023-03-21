using System.Reflection;
using AkoSharp;
using Borz.Core.Generators;
using Borz.Core.Platform;
using ByteSizeLib;

namespace Borz.Core;

public static class Borz
{
    public static void Init()
    {
        MugiLog.Init();

        ShortTypeRegistry.Init();

        var configFolder = Path.Combine(IPlatform.Instance.GetUserConfigPath(), "borz");
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        var configFile = Path.Combine(configFolder, "config.ako");

        //Load defaults into config
        SetupDefaults();

        if (File.Exists(configFile))
        {
            Deserializer.FromString(Config.GetLayer(ConfigLayers.LayerType.UserGobal), File.ReadAllText(configFile));
        }
        else
        {
            File.WriteAllText(configFile, "# Borz Configuration File\ntemp;");
        }

        ConfigChanged();
    }

    public static void Shutdown()
    {
        MugiLog.Shutdown();
    }


    public static ConfigLayers Config = new();

    public static ParallelOptions ParallelOptions = new();

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
            Deserializer.FromString(Config.GetLayer(ConfigLayers.LayerType.Defaults), result);
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

        var memoryInfo = IPlatform.Instance;
        var totalMemory = memoryInfo.GetTotalMemory();
        var availableMemory = memoryInfo.GetAvailableMemory();

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

    public static void CompileWorkspace(string workspacePath, bool justLog = false)
    {
        //Set the current directory to the workspace path
        Directory.SetCurrentDirectory(workspacePath);

        //This runs the build.borz command in the current directory.
        Workspace.Init();

        Core.Borz.UpdateMemInfo();
        Workspace.Projects.ForEach(prj =>
        {
            MugiLog.Info("===========================================");
            var builder = IBuilder.GetBuilder(prj);
            builder.Build(prj, justLog);
        });
    }

    public static void GenerateWorkspace(string workspacePath)
    {
        Directory.SetCurrentDirectory(workspacePath);
        Workspace.Init();

        IGenerator generator = new CMakeGenerator();
        generator.Generate();
    }
}