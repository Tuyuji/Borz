using Borz.Core.Lua;
using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class BuildConfig
{
    //Debug, Release....
    private string _config = "debug";
    private Platform _targetPlatform = Platform.Unknown;

    public string Config
    {
        get => _config;
        set => _config = value.ToLower();
    }

    public Platform TargetPlatform
    {
        get => _targetPlatform;
        set => _targetPlatform = value;
    }

    public Platform HostPlatform => Util.getHostPlatform();

    public bool ConfigEquals(string config)
    {
        return String.Equals(Config, config, StringComparison.InvariantCultureIgnoreCase);
    }
}