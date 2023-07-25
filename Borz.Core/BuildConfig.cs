using Borz.Core;
using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class BuildConfig
{
    //Debug, Release....
    private string _config = "debug";
    private string _targetPlatform = "host";

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

    public PlatformInfo TargetInfo => Platform.GetInfo(Core.Borz.BuildConfig.TargetPlatform);

    public string HostPlatform => Util.getHostPlatform();

    public bool ConfigEquals(string config)
    {
        return String.Equals(Config, config, StringComparison.InvariantCultureIgnoreCase);
    }
}