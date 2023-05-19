using MoonSharp.Interpreter;

namespace Borz.Core;

[MoonSharpUserData]
public class WorkspaceSettings
{
    public List<string> Configs = new();

    public WorkspaceSettings()
    {
        Configs.Add("debug");
        Configs.Add("release");
    }
}