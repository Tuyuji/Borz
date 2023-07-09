namespace Borz.Core;

public static class Utils
{
    public static T? CreateInstance<T>(this Type type, params object?[]? args)
    {
        return (T?)Activator.CreateInstance(type, args);
    }

    public static UnixUtil.RunOutput RunCmd(string command, string args, string workingDir = "", bool _justLog = false)
    {
        Borz.BuildLog.Enqueue($"RunCmd: {command} {args}");
        if (!_justLog) return UnixUtil.RunCmd(command, args, workingDir);
        MugiLog.Info($"{command} {args}");
        return new UnixUtil.RunOutput(String.Empty, String.Empty, 0);
    }

    public static string StandardReplace(string input)
    {
        return input
            .Replace("$WORKSPACEDIR", Workspace.Location)
            .Replace("$CONFIG", Borz.BuildConfig.Config)
            .Replace("$TARGETPLATFORM", Borz.BuildConfig.TargetPlatform.ToString());
    }

    public static string StandardProjectReplace(string input, string projectDir, string projectName)
    {
        return StandardReplace(input)
            .Replace("$PROJECTDIR", projectDir)
            .Replace("$PROJECTNAME", projectName);
    }

    public static PlatformInfo GetTargetPlatformInfo()
    {
        switch (Borz.BuildConfig.TargetPlatform)
        {
            case Lua.Platform.Android:
            case Lua.Platform.Linux:
                return new PlatformInfo()
                {
                    ExecutablePrefix = "",
                    ExecutableExtension = "",

                    SharedLibraryPrefix = "lib",
                    SharedLibraryExtension = ".so",

                    StaticLibraryPrefix = "lib",
                    StaticLibraryExtension = ".a"
                };
            case Lua.Platform.iOS:
            case Lua.Platform.MacOS:
                return new PlatformInfo()
                {
                    ExecutablePrefix = "",
                    ExecutableExtension = "",

                    SharedLibraryPrefix = "lib",
                    SharedLibraryExtension = ".dylib",

                    StaticLibraryPrefix = "lib",
                    StaticLibraryExtension = ".a"
                };
            case Lua.Platform.Windows:
                return new PlatformInfo()
                {
                    ExecutablePrefix = "",
                    ExecutableExtension = ".exe",

                    SharedLibraryPrefix = "",
                    SharedLibraryExtension = ".dll",

                    StaticLibraryPrefix = "",
                    StaticLibraryExtension = ".lib"
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static string AddPlatformIfixsToFileName(string filename, BinType type)
    {
        var info = GetTargetPlatformInfo();
        switch (type)
        {
            case BinType.ConsoleApp:
                return $"{info.ExecutablePrefix}{filename}{info.ExecutableExtension}";
            case BinType.SharedObj:
                return $"{info.SharedLibraryPrefix}{filename}{info.SharedLibraryExtension}";
            case BinType.StaticLib:
                return $"{info.StaticLibraryPrefix}{filename}{info.StaticLibraryExtension}";
            case BinType.WindowsApp:
                return $"{info.ExecutablePrefix}{filename}{info.ExecutableExtension}";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}