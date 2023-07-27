using System.Collections.Concurrent;
using AkoSharp;

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

    private static ConcurrentDictionary<string, PlatformInfo> _knownPlatformInfos = new();

    private static AkoVar? ConfigGetPlatformInfo(string platform, params string[] keys)
    {
        List<string> query = new() { "platform", "info", platform };
        query.AddRange(keys);
        return Borz.Config.Get(query.ToArray());
    }

    public static PlatformInfo GetInfo(string platform)
    {
        if (_knownPlatformInfos.TryGetValue(platform, out var info))
            return info;

        //see if we cant get it from config
        if (Borz.Config.Get("platform", "info", platform) is { } conf)
        {
            if (conf.Type != AkoVar.VarType.TABLE)
                throw new Exception($"Platform info for {platform} is not a table");

            info = new PlatformInfo
            {
                ExecutablePrefix = ConfigGetPlatformInfo(platform, "exe", "prefix") ?? "",
                ExecutableExtension = ConfigGetPlatformInfo(platform, "exe", "suffix") ?? "",

                SharedLibraryPrefix = ConfigGetPlatformInfo(platform, "sharedlib", "prefix") ?? "",
                SharedLibraryExtension = ConfigGetPlatformInfo(platform, "sharedlib", "suffix") ?? "",

                StaticLibraryPrefix = ConfigGetPlatformInfo(platform, "staticlib", "prefix") ?? "",
                StaticLibraryExtension = ConfigGetPlatformInfo(platform, "staticlib", "suffix") ?? ""
            };

            //worked so lets add it to the list
            _knownPlatformInfos.TryAdd(platform, info);
            return info;
        }

        throw new Exception($"Unknown platform {platform}");
    }

    public static void RegisterPlatform(string platform, PlatformInfo info)
    {
        if (!_knownPlatformInfos.TryAdd(platform, info))
        {
            throw new Exception($"Platform {platform} already registered");
        }
    }
}