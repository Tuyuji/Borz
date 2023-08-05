using System.Reflection;
using MoonSharp.Interpreter;

namespace Borz.Core;

[MoonSharpUserData]
public abstract class Project
{
    public static Dictionary<string, Type> ProjectTypes = new();

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
    public string Language;
    public string OutputName;

    public List<Project> Dependencies = new();

    public string OutputDirectory
    {
        get => _outputDir;
        set => _outputDir = Utils.StandardProjectReplace(value, ProjectDirectory, Name);
    }

    public string IntermediateDirectory
    {
        get => _intDir;
        set => _intDir = Utils.StandardProjectReplace(value, ProjectDirectory, Name);
    }

    public event EventHandler FinishedCompiling;

    private string _outputDir;
    private string _intDir;

    public void CallFinishedCompiling()
    {
        FinishedCompiling?.Invoke(this, EventArgs.Empty);
    }

    public string GetOutputName()
    {
        return Utils.StandardReplace(OutputName).Replace("$NAME", Name);
    }

    public Project(string name, BinType type, string language, string directory = "", bool addToWorkspace = true)
    {
        if (directory == string.Empty)
            directory = Directory.GetCurrentDirectory();
        OutputName = Borz.Config.Get("project", "output");

        ProjectDirectory = directory;
        Name = name;
        Type = type;
        Language = language;

        OutputDirectory = Borz.Config.Get("paths", "output");
        IntermediateDirectory = Borz.Config.Get("paths", "int");

        if (OutputDirectory == "")
            MugiLog.Fatal("Output directory is empty");
        if (IntermediateDirectory == "")
            MugiLog.Fatal("Intermediate directory is empty");

        if (addToWorkspace)
            Workspace.Projects.Add(this);
    }

    public static dynamic Create(Script script, string name, BinType type, string language)
    {
        var t = ProjectTypes[language];
        //we need to call a static method on the type called Create
        var method = t.GetMethod("Create");
        if (method == null)
            throw new Exception("Could not find Create method on type " + t.Name);
        var p = (Project?)method.Invoke(null, new object[] { script, name, type });
        if (p == null)
            throw new Exception("Create method on type " + t.Name + " returned null");

        var projectCallback = script.Globals["OnProjectCreate"];
        if (projectCallback is Closure pcd) pcd.Call(p);

        return p;
    }

    public void AddDep(Project project)
    {
        if (project == null)
            throw new Exception("Cannot add null project as dependency");

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
        for (var i = 0; i < paths.Length; i++) absPaths[i] = GetPathAbs(paths[i]);

        return absPaths;
    }

    public string GetOutputFilePath()
    {
        var outputName = GetOutputName();
        var outputFileName = Utils.AddPlatformIfixsToFileName(outputName, Type);
        return Path.Combine(OutputDirectory, outputFileName);
    }
}