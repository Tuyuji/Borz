using System.Collections.Concurrent;
using System.Reflection;
using AkoSharp;
using Borz.Core.Generators;
using Borz.Core.Lua;
using Borz.Core.Platform;
using ByteSizeLib;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace Borz.Core;

public static class Borz
{
    public static ConcurrentDictionary<string, Action> Generators = new();
    public static ConfigLayers<ConfLevel> Config = new();
    public static ParallelOptions ParallelOptions = new();
    public static ConcurrentQueue<string> BuildLog = new();
    public static Script Script;

    public static bool UseMold
    {
        get
        {
            var use = false;
            var conf = Config.Get("linker", "mold");
            if (conf == true)
                use = true;
            return use;
        }
    }

    static Borz()
    {
        Generators["cmake"] = () => { new CMakeGenerator().Generate(); };
        Generators["jetbrains"] = () => { new JetbrainsGenerator().Generate(); };
        Generators["sublime"] = () => { new SublimeGenerator().Generate(); };
    }

    public static void Init()
    {
        MugiLog.Init();

        ShortTypeRegistry.AutoRegister();

        var configFolder = Path.Combine(IPlatform.Instance.GetUserConfigPath(), "borz");
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        var configFile = Path.Combine(configFolder, "config.ako");

        //Load defaults into config
        SetupDefaults();

        if (File.Exists(configFile))
            Deserializer.FromString(Config.GetLayer(ConfLevel.UserGobal), File.ReadAllText(configFile));
        else
            File.WriteAllText(configFile, "# Borz Configuration File\ntemp;");

        ConfigChanged();

        Project.Setup();
        ((ScriptLoaderBase)Script.DefaultOptions.ScriptLoader).ModulePaths = new string[] { "./?", "./?.lua" };
        ScriptRunner.RegisterTypes();
        Script = ScriptRunner.CreateScript();

        var userScripts = Path.Combine(configFolder, "scripts");
        if (Directory.Exists(userScripts))
        {
            //load up main.lua
            var mainLua = Path.Combine(userScripts, "main.lua");
            if (File.Exists(mainLua)) RunScript(mainLua);
        }
    }

    public static void Shutdown()
    {
        MugiLog.Shutdown();

        var shouldWriteBuildLog = Config.Get("debug", "write_log_to_temp");
        if (shouldWriteBuildLog != true) return;

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


    private static void SetupDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Borz.Core.defaults.ako";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            var result = reader.ReadToEnd();
            Deserializer.FromString(Config.GetLayer(ConfLevel.Defaults), result);
        }
    }

    public static void ConfigChanged()
    {
        {
            var levelStr = Config.Get("log", "level");
            var level = LogLevel.Info;
            if (!Enum.TryParse(levelStr, true, out level))
                MugiLog.Error("Failed to parse log level from config, defaulting to Info.");

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


        ParallelOptions.MaxDegreeOfParallelism = usableThreadCount;
    }

    public static void RunScript(string location)
    {
        var fullPath = Path.GetFullPath(location);
        var dir = Path.GetDirectoryName(fullPath);

        Script.SetCwd(dir!);
        try
        {
            Script.DoFile(fullPath);
        }
        catch (Exception exception)
        {
            if (exception is InterpreterException runtimeError)
                MugiLog.Fatal(runtimeError.DecoratedMessage);
            else
                MugiLog.Fatal(exception.Message);

            MugiLog.Wait();
            MugiLog.Shutdown();
            //rethrow
            throw;
        }
    }
}