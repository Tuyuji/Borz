using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class BuildConfig
{
    //Debug, Release....
    private string _config = "debug";

    public string Config
    {
        get => _config;
        set => _config = value.ToLower();
    }

    public bool ConfigEquals(string config)
    {
        return String.Equals(Config, config, StringComparison.InvariantCultureIgnoreCase);
    }
}