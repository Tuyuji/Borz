using System.Reflection;
using AkoSharp;
using Borz.Lua;
using ByteSizeLib;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace Borz;

public static class Borz
{
    public static ConfigLayers<ConfLevel> Config = new();

    public static string ConfigFolderPath => Path.Combine(IPlatform.Instance.GetUserConfigPath(), "borz");
    
    public static void Init()
    {
        MugiLog.Init();
        
        SetupDefaults(); //Load defaults into Config
        LoadUserConfig(); //Load user config into Config
        
        IPlatform.Instance.Init();
        
        //safe to assume that the users preferred log level is set in Config.
        //but the environment variable is always preferred over the config.
        var loglevel = Environment.GetEnvironmentVariable("BORZ_LL");
        if (loglevel == null)
        {
            loglevel = Config.Get("log", "level");
            SetLogLevelFromString(loglevel);
        }
        else
        {
            SetLogLevelFromString(loglevel);
        }
        
        ((ScriptLoaderBase)Script.DefaultOptions.ScriptLoader).ModulePaths = new string[] { "./?", "./?.lua" };
        ScriptRunner.RegisterTypes();
        
        //Call scripts in user dir
        if (Directory.Exists(Path.Combine(ConfigFolderPath, "scripts")))
        {
            var script = ScriptRunner.CreateScript();
            ScriptRunner.Eval(script,Path.Combine(ConfigFolderPath, "scripts", "main.lua"));
        }

    }

    public static void Shutdown()
    {
        MugiLog.Shutdown();
    }

    private static void SetLogLevelFromString(string loglevel)
    {
        var level = LogLevel.Info;
        if (Enum.TryParse(loglevel, true, out level))
        {
            MugiLog.MinLevel = level;
        }
    }
    
    private static void SetupDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Borz.defaults.ako";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            var result = reader.ReadToEnd();
            Deserializer.FromString(Config.GetLayer(ConfLevel.Defaults), result);
        }
    }

    private static void LoadUserConfig()
    {
        var configPath = ConfigFolderPath;
        if (!Directory.Exists(configPath))
            Directory.CreateDirectory(configPath);
        
        var configFilePath = Path.Combine(configPath, "config.ako");

        if (File.Exists(configFilePath))
            Deserializer.FromString(Config.GetLayer(ConfLevel.UserGobal), File.ReadAllText(configFilePath));
        else
            File.WriteAllText(configFilePath, "# Borz Configuration File\ntemp;");
    }
    
    public static int GetUsableThreadCount()
    {
        //String in format like "1G" or "2M" or "3K"
        var perThreadMinMemory = (string?)Config.Get("mt", "minThreadMem");
        var perThreadMinMemoryGB = ByteSize.Parse(perThreadMinMemory).GigaBytes;

        var maxCpuCount = Environment.ProcessorCount;

        var maxReqThreads = (int)Config.Get("mt", "maxThreads");
        if (maxReqThreads == -1 || maxReqThreads == 0)
            //Use cpu max
            maxReqThreads = maxCpuCount;

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
        return usableThreadCount;
    }
}