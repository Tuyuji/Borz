using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public abstract class Project
{
    public string ProjectDirectory;
    public string Name;
    public BinType Type;
    public Language Language;

    public List<Project> Dependencies = new();

    public string OutputDirectory;
    public string IntermediateDirectory;

    public event EventHandler FinishedCompiling;

    public void CallFinishedCompiling() => FinishedCompiling(this, EventArgs.Empty);

    public Project(string name, BinType type, Language language, string directory = "")
    {
        if (directory == String.Empty)
            directory = Directory.GetCurrentDirectory();

        OutputDirectory = Utils.Config.Get("paths", "output");
        IntermediateDirectory = Utils.Config.Get("paths", "int");

        //REPLACE $PROJECTDIR with the project directory
        OutputDirectory = OutputDirectory.Replace("$PROJECTDIR", directory);
        IntermediateDirectory = IntermediateDirectory.Replace("$PROJECTDIR", directory);

        //Replace $WORKSPACEDIR with the workspace directory
        OutputDirectory = OutputDirectory.Replace("$WORKSPACEDIR", Workspace.Location);
        IntermediateDirectory = IntermediateDirectory.Replace("$WORKSPACEDIR", Workspace.Location);

        //Replace $PROJECTNAME with the project name
        OutputDirectory = OutputDirectory.Replace("$PROJECTNAME", name);
        IntermediateDirectory = IntermediateDirectory.Replace("$PROJECTNAME", name);

        if (OutputDirectory == "")
            MugiLog.Fatal("Output directory is empty");
        if (IntermediateDirectory == "")
            MugiLog.Fatal("Intermediate directory is empty");


        this.ProjectDirectory = directory;
        this.Name = name;
        this.Type = type;
        this.Language = language;

        Workspace.Projects.Add(this);
    }

    public void AddDep(Project project)
    {
        if(Dependencies.Contains(project))
            return;
        
        Dependencies.Add(project);
    }

    public string GetPathAbs(string path)
    {
        //See if path is absolute
        if (Path.IsPathRooted(path))
            return path;
        
        //If not, make it absolute
        return Path.Combine(ProjectDirectory, path);
    }
    
    public string[] GetPathsAbs(string[] paths)
    {
        var absPaths = new string[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            absPaths[i] = GetPathAbs(paths[i]);
        }

        return absPaths;
    }

}