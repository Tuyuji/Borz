using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public class Options
{
    public List<string> ValidConfigs = ["debug", "release"];
    public string Config;

    public bool JustPrint = false;
    
    public MachineInfo Host;
    //If its null assume the same as the host
    public MachineInfo? Target = null;

    //Known projects built using this config
    public List<Project> BuiltProjects = new();
    
    public Options()
    {
        Config = ValidConfigs[0];
        Host = MachineInfo.GetCurrentMachineInfo();
    }

    public MachineInfo GetTarget()
    {
        return Target ?? Host;
    }

    public bool HasProjectBeenBuilt(Project? project)
    {
        return project != null && BuiltProjects.Contains(project);
    }

    public void SetProjectBuilt(Project project)
    {
        if (BuiltProjects.Contains(project))
            return;
        BuiltProjects.Add(project);
    }

    public void ClearBuiltProjects()
    {
        BuiltProjects.Clear();
    }
}