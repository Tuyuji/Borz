using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class BuildConfig
{
    //Debug, Release....
    public string Config { get; set; } = "debug";
}