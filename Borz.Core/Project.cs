using System.Reflection;
using MoonSharp.Interpreter;

namespace Borz.Core;

[MoonSharpUserData]
public abstract class Project
{
    public static Dictionary<Language, Type> ProjectTypes = new();

    public static void Setup()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(e => e.GetCustomAttribute<ProjectLanguageAttribute>() != null)
            .Where(e => e.IsAssignableTo(typeof(Project)))
            .ToArray();

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<ProjectLanguageAttribute>();
            ProjectTypes.Add(attr!.Language, type);
        }
    }


    public string ProjectDirectory;
    public string Name;
    public BinType Type;
    public Language Language;

    public List<Project> Dependencies = new();

    public string OutputDirectory;
    public string IntermediateDirectory;

    public event EventHandler FinishedCompiling;

    public void CallFinishedCompiling() => FinishedCompiling?.Invoke(this, EventArgs.Empty);

    public Project(string name, BinType type, Language language, string directory = "", bool addToWorkspace = true)
    {
        if (directory == String.Empty)
            directory = Directory.GetCurrentDirectory();

        OutputDirectory = Borz.Config.Get("paths", "output");
        IntermediateDirectory = Borz.Config.Get("paths", "int");

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

        if (addToWorkspace)
            Workspace.Projects.Add(this);
    }

    public static dynamic Create(Script script, string name, BinType type, Language language)
    {
        var t = ProjectTypes[language];
        //we need to call a static method on the type called Create
        var method = t.GetMethod("Create");
        if (method == null)
            throw new Exception("Could not find Create method on type " + t.Name);
        var p = (Project?)method.Invoke(null, new object[] { script, name, type });
        if (p == null)
            throw new Exception("Create method on type " + t.Name + " returned null");
        return p;
    }

    public void AddDep(Project project)
    {
        if (Dependencies.Contains(project))
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