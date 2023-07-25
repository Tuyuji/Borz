namespace Borz.Core.Lua;

[BorzUserData]
public static class Platform
{
    public const string Android = "android";
    public const string Linux = "linux";
    public const string iOS = "ios";
    public const string MacOS = "macos";
    public const string Windows = "windows";
    public const string WebAssembly = "wasm";
    public const string Unknown = "unknown";

    private static Dictionary<string, PlatformInfo> _knownPlatformInfos = new();

    static Platform()
    {
        var linuxy = new PlatformInfo()
        {
            ExecutablePrefix = "",
            ExecutableExtension = "elf",

            SharedLibraryPrefix = "lib",
            SharedLibraryExtension = ".so",

            StaticLibraryPrefix = "lib",
            StaticLibraryExtension = ".a",
        };

        var apple = new PlatformInfo()
        {
            ExecutablePrefix = "",
            ExecutableExtension = "",

            SharedLibraryPrefix = "lib",
            SharedLibraryExtension = ".dylib",

            StaticLibraryPrefix = "lib",
            StaticLibraryExtension = ".a"
        };

        var windows = new PlatformInfo()
        {
            ExecutablePrefix = "",
            ExecutableExtension = ".exe",

            SharedLibraryPrefix = "",
            SharedLibraryExtension = ".dll",

            StaticLibraryPrefix = "",
            StaticLibraryExtension = ".lib"
        };

        _knownPlatformInfos.Add(Android, linuxy);
        _knownPlatformInfos.Add(Linux, linuxy);

        _knownPlatformInfos.Add(iOS, apple);
        _knownPlatformInfos.Add(MacOS, apple);

        _knownPlatformInfos.Add(Windows, windows);
    }

    public static PlatformInfo GetInfo(string platform)
    {
        if (_knownPlatformInfos.TryGetValue(platform, out var info))
            return info;

        throw new Exception($"Unknown platform {platform}");
    }

    public static void RegisterPlatform(string platform, PlatformInfo info)
    {
        _knownPlatformInfos.Add(platform, info);
    }
}