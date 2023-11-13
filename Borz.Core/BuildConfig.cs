using Borz.Core;
using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class BuildConfig
{
    public List<string> ValidConfigs = new()
    {
        "debug",
        "release"
    };

    public BuildConfig()
    {
        ValidConfigs.Add("debug");
        ValidConfigs.Add("release");
    }

    public string Config
    {
        get => _config;
        set => _config = value.ToLower();
    }

    public string TargetPlatform
    {
        get => _targetPlatform;
        set => _targetPlatform = value;
    }

    public PlatformInfo TargetInfo => Platform.GetInfo(TargetPlatform);

    public string HostPlatform => Util.getHostPlatform();

    //Debug, Release....
    private string _config = "debug";
    private string _targetPlatform = Platform.Unknown;

    public bool SetConfig(string newConfig)
    {
        if (!ValidConfigs.Contains(newConfig)) return false;
        Config = newConfig;
        return true;
    }

    public bool ConfigEquals(string config)
    {
        return string.Equals(Config, config, StringComparison.InvariantCultureIgnoreCase);
    }
}