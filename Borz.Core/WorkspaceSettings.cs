using MoonSharp.Interpreter;

namespace Borz.Core;

[MoonSharpUserData]
public class WorkspaceSettings
{
    public string Name = "Workspace";
    public List<string> Configs = new();

    public WorkspaceSettings()
    {
        Configs.Add("debug");
        Configs.Add("release");
    }
}