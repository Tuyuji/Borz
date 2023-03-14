using System.Reflection;
using AkoSharp;
using Borz.Platform;
using ByteSizeLib;
using Spectre.Console;

namespace Borz;

public static class Utils
{
    public static ConfigLayers Config = new();
    
    public static ParallelOptions ParallelOptions = new();

    private static void SetupDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Borz.defaults.ako";
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            string result = reader.ReadToEnd();
            Deserializer.FromString(Config.GetLayer(ConfigLayers.LayerType.Defaults), result);
        }
        
    }

    public static void Init()
    {
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
            
        if(maxReqThreads > maxCpuCount)
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
        if(usableThreadCount > maxCpuCount)
            usableThreadCount = maxCpuCount;
            
        MugiLog.Debug("Total Memory: " + totalMemory);
        MugiLog.Debug("Available Memory: " + availableMemory);
        MugiLog.Debug("Per Thread Min Memory: " + perThreadMinMemory);
        MugiLog.Debug("Max Threads: " + maxReqThreads);
            
        MugiLog.Debug($"Using {usableThreadCount} threads, " +
                      $"({availableMemoryGB:F2}GB/{perThreadMinMemoryGB:F2}GB = " +
                      $"{availableMemoryGB/perThreadMinMemoryGB:F2})");
            
            
            
        ParallelOptions.MaxDegreeOfParallelism = usableThreadCount;
    }
    
    public static T? CreateInstance<T>(this Type type, params object?[]? args)
    {
        return (T?)Activator.CreateInstance(type, args);
    }

    public static UnixUtil.RunOutput RunCmd(string command, string args, string workingDir = "", bool _justLog = false)
    {
        if (!_justLog) return UnixUtil.RunCmd(command, args, workingDir);
        MugiLog.Info($"{command} {args}");
        return new UnixUtil.RunOutput(String.Empty, String.Empty, 0);

    }
}