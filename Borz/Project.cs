using Borz.Languages.C;
using Borz.Languages.D;
using Borz.Lua;
using MoonSharp.Interpreter;

namespace Borz;

[MoonSharpUserData]
public abstract class Project
{
    public delegate Project ProjectionCreateDelegate(string name, BinType type, string directory);
    
    public static Dictionary<string, ProjectionCreateDelegate> KnownProjectTypes = new()
    {
        { Lang.C, (name, type, directory) => new CProject(name, type, directory) },
        { Lang.Cpp, (name, type, directory) => new CppProject(name, type, directory) },
        { Lang.D, (name, type, directory) => new DProject(name, type, directory) },
    };

    public Workspace? Owner = null;
    
    public string Name;
    public string OutputNameTemplate = string.Empty;
    public BinType Type;
    public string Directory;
    public string Language = string.Empty;
    public List<string> Tags = new();
    public string License = string.Empty;
    public List<string> LicenseFiles = new();

    public List<Project> Dependencies = new();

    public string OutputDirectory = string.Empty;
    public string IntermediateDirectory = string.Empty;
    
    public Project(string name, BinType type, string directory)
    {
        Name = name;
        Type = type;
        Directory = directory;

        if (!Path.IsPathRooted(directory))
        {
            throw new Exception("Given project directory is not absolute");
        }
    }

    public static Project Create(string lang, string name, BinType type, string directory)
    {
        lang = lang.ToLower();
        if(!KnownProjectTypes.ContainsKey(lang))
            throw new Exception("Failed to create project with language: " + lang);

        return KnownProjectTypes[lang](name, type, directory);
    }
    
    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }

    public void AddDep(Project project)
    {
        if (project == null)
            throw new ScriptRuntimeException("Cannot add null project as dependency");

        if (Dependencies.Contains(project))
            return;

        Dependencies.Add(project);
    }
    
    private string ResolveTemplate(string optTemplate, string defaultTemplate)
    {
        string workingTemplate = optTemplate;
        if (workingTemplate == String.Empty)
        {
            workingTemplate = defaultTemplate;
        }
        
        //Make sure no unexpect behaviour happens.
        if (Owner == null && workingTemplate.Contains("$WORKSPACE"))
        {
            throw new Exception("Using template $WORKSPACE when project doesn't belong to a workspace.");
        }
        
        if(Owner != null)
        {
            workingTemplate = workingTemplate.Replace("$WORKSPACE", Owner.Location);
        }

        return workingTemplate
            .Replace("$PROJECTNAME", Name)
            .Replace("$PROJECTDIR", Directory);
    }

    private string ResolveTemplateConfig(string optTemplate, string defaultTemplate, Options opt)
    {
        return ResolveTemplate(optTemplate, defaultTemplate)
            .Replace("$CONFIG", opt.Config)
            .Replace("$TARGET_OS", opt.GetTarget().OS)
            .Replace("$ARCH", opt.GetTarget().Arch)
            .Replace("$TARGET", opt.GetTarget().ToString());
    }

    /// <summary>
    /// Fills the template for either the output path in the project if it exists
    /// or the defaultPaths.
    /// </summary>
    /// <param name="defaultPath">Should be Config.Get("paths", "output")</param>
    /// <returns></returns>
    public string GetOutputDirectory(string defaultTemplate, Options opt)
    {
        return ResolveTemplateConfig(OutputDirectory, defaultTemplate, opt);
    }

    public string GetOutputDirectory(Options opt) => GetOutputDirectory(Borz.Config.Get("paths", "output"), opt);

    public string GetIntermediateDirectory(string defaultTemplate, Options opt)
    {
        return ResolveTemplateConfig(IntermediateDirectory, defaultTemplate, opt);
    }

    public string GetIntermediateDirectory(Options opt) => GetIntermediateDirectory(Borz.Config.Get("paths", "int"), opt);

    public string GetOutputName(string defaultTemplate, Options opt)
    {
        return ResolveTemplateConfig(OutputNameTemplate, defaultTemplate, opt);
    }

    public string GetOutputName(Options opt) => GetOutputName(Borz.Config.Get("project", "outputTmpl"), opt);

    //Note: does use the global borz config for the default output template if its not defined in project.
    public string GetOutputFilePath(Options opt)
    {
        var outputName = GetOutputName(opt);
        if (Path.HasExtension(outputName))
        {
            return Path.Combine(GetOutputDirectory(opt), outputName);
        }
        var outputFileName = Utils.AddMachineIfixsToFileName(outputName, Type, opt.GetTarget());
        return Path.Combine(GetOutputDirectory(opt), outputFileName);
    }

    public List<string> GetLicenseFilesAbs()
    {
        return LicenseFiles.Select(GetPathAbs).ToList();
    }
    
    public string GetPathAbs(string input)
    {
        return Path.GetFullPath(input, Directory);
    }
    
    public string[] GetPathsAbs(string[] paths)
    {
        var absPaths = new string[paths.Length];
        for (var i = 0; i < paths.Length; i++) absPaths[i] = GetPathAbs(paths[i]);

        return absPaths;
    }
}