using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Borz.Generators;

public class JetbrainsGenerator : Generator
{
    public class ExternalTools
    {
        [XmlRoot(ElementName = "option")]
        public class Option
        {
            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }

            public Option(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public Option()
            {
            }
        }

        [XmlRoot(ElementName = "exec")]
        public class Exec
        {
            [XmlElement(ElementName = "option")] public List<Option> Option { get; set; }
        }

        [XmlRoot(ElementName = "tool")]
        public class Tool
        {
            [XmlElement(ElementName = "exec")] public Exec Exec { get; set; }

            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

            [XmlAttribute(AttributeName = "showInMainMenu")]
            public bool ShowInMainMenu { get; set; }

            [XmlAttribute(AttributeName = "showInEditor")]
            public bool ShowInEditor { get; set; }

            [XmlAttribute(AttributeName = "showInProject")]
            public bool ShowInProject { get; set; }

            [XmlAttribute(AttributeName = "showInSearchPopup")]
            public bool ShowInSearchPopup { get; set; }

            [XmlAttribute(AttributeName = "disabled")]
            public bool Disabled { get; set; }

            [XmlAttribute(AttributeName = "useConsole")]
            public bool UseConsole { get; set; }

            [XmlAttribute(AttributeName = "showConsoleOnStdOut")]
            public bool ShowConsoleOnStdOut { get; set; }

            [XmlAttribute(AttributeName = "showConsoleOnStdErr")]
            public bool ShowConsoleOnStdErr { get; set; }

            [XmlAttribute(AttributeName = "synchronizeAfterRun")]
            public bool SynchronizeAfterRun { get; set; }
        }

        [XmlRoot(ElementName = "toolSet")]
        public class ToolSet
        {
            [XmlElement(ElementName = "tool")] public List<Tool> Tool { get; set; }

            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }
        }
    }

    public class CustomTargets
    {
        [XmlRoot(ElementName = "tool")]
        public class Tool
        {
            [XmlAttribute(AttributeName = "actionId")]
            public string ActionId { get; set; }
        }

        [XmlRoot(ElementName = "build")]
        public class Build
        {
            [XmlElement(ElementName = "tool")] public Tool Tool { get; set; }

            [XmlAttribute(AttributeName = "type")] public string Type { get; set; }
        }

        [XmlRoot(ElementName = "clean")]
        public class Clean
        {
            [XmlElement(ElementName = "tool")] public Tool Tool { get; set; }

            [XmlAttribute(AttributeName = "type")] public string Type { get; set; }
        }

        [XmlRoot(ElementName = "configuration")]
        public class Configuration
        {
            [XmlElement(ElementName = "build")] public Build Build { get; set; }

            [XmlElement(ElementName = "clean")] public Clean Clean { get; set; }

            [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }
        }

        [XmlRoot(ElementName = "target")]
        public class Target
        {
            [XmlElement(ElementName = "configuration")]
            public Configuration Configuration { get; set; }

            [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

            [XmlAttribute(AttributeName = "defaultType")]
            public string DefaultType { get; set; }
        }

        [XmlRoot(ElementName = "component")]
        public class Component
        {
            [XmlElement(ElementName = "target")] public List<Target> Target { get; set; }

            [XmlAttribute(AttributeName = "name")] public string Name { get; set; }
        }

        [XmlRoot(ElementName = "project")]
        public class Project
        {
            [XmlElement(ElementName = "component")]
            public Component Component { get; set; }

            [XmlAttribute(AttributeName = "version")]
            public int Version { get; set; }
        }
    }


    private static ExternalTools.Tool CompileTool;
    private static ExternalTools.Tool CleanTool;

    private static CustomTargets.Target BorzTarget;

    private static XmlWriterSettings CommonSettings = new()
        { OmitXmlDeclaration = true, Indent = true };

    static JetbrainsGenerator()
    {
        var processPath = Environment.ProcessPath ?? "$USER_HOME$/.local/bin/borz";
        CompileTool = new ExternalTools.Tool()
        {
            Name = "Borz Compile",
            ShowInMainMenu = false,
            ShowInEditor = false,
            ShowInProject = false,
            ShowInSearchPopup = false,
            Disabled = false,
            UseConsole = true,
            ShowConsoleOnStdOut = false,
            ShowConsoleOnStdErr = false,
            SynchronizeAfterRun = true,
            Exec = new ExternalTools.Exec()
            {
                Option = new List<ExternalTools.Option>(new[]
                {
                    new ExternalTools.Option("COMMAND", processPath),
                    new ExternalTools.Option("PARAMETERS", "c"),
                    new ExternalTools.Option("WORKING_DIRECTORY", "$ProjectFileDir$")
                })
            }
        };
        
        CleanTool = new ExternalTools.Tool()
        {
            Name = "Borz Clean",
            ShowInMainMenu = false,
            ShowInEditor = false,
            ShowInProject = false,
            ShowInSearchPopup = false,
            Disabled = false,
            UseConsole = true,
            ShowConsoleOnStdOut = false,
            ShowConsoleOnStdErr = false,
            SynchronizeAfterRun = true,
            Exec = new ExternalTools.Exec()
            {
                Option = new List<ExternalTools.Option>(new[]
                {
                    new ExternalTools.Option("COMMAND", processPath),
                    new ExternalTools.Option("PARAMETERS", "clean"),
                    new ExternalTools.Option("WORKING_DIRECTORY", "$ProjectFileDir$")
                })
            }
        };

        BorzTarget = new CustomTargets.Target()
        {
            Name = "Borz",
            DefaultType = "TOOL",
            Id = "217343d4-e8c6-4461-ac30-cf0a72cf6737",
            Configuration = new CustomTargets.Configuration()
            {
                Name = "Borz",
                Id = "f6447281-abde-4fbe-a79b-5bab6f6e2f11",
                Build = new CustomTargets.Build()
                {
                    Type = "TOOL",
                    Tool = new CustomTargets.Tool()
                    {
                        ActionId = "Tool_External Tools_Borz Compile"
                    }
                },
                Clean = new CustomTargets.Clean()
                {
                    Type = "TOOL",
                    Tool = new CustomTargets.Tool()
                    {
                        ActionId = "Tool_External Tools_Borz Clean"
                    }
                }
            }
        };
    }
    
    private static void AssureFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    
    public void HandleExternalTools(string tools)
    {
        var externalToolsPath = Path.Combine(tools, "External Tools.xml");

        //in tools theres a file called "External Tools.xml"
        //If it exists then we need to open it and see if Borz Build and Borz Clean are there
        if (File.Exists(externalToolsPath))
            if (UpdateExisting(externalToolsPath))
                return;

        //well we couldn't open it or it doesnt exist, good enough reason to overwrite it i guess?
        var serializer = new XmlSerializer(typeof(ExternalTools.ToolSet));
        var toolSet = new ExternalTools.ToolSet()
        {
            Name = "External Tools",
            Tool = new List<ExternalTools.Tool>()
            {
                CompileTool,
                CleanTool
            }
        };
        WriteXml(externalToolsPath, toolSet);
    }
    
    
    public void HandleCustomTargets(string idea)
    {
        var customTargetsPath = Path.Combine(idea, "customTargets.xml");
        if (File.Exists(customTargetsPath))
        {
            //open
            var serializer = new XmlSerializer(typeof(CustomTargets.Project));
            using var reader = new StreamReader(customTargetsPath);
            var project = (CustomTargets.Project)serializer.Deserialize(reader);
            //if our target already exists then we dont need to do anything
            if (project.Component.Target.Any(t => t.Id == BorzTarget.Id))
                return;

            //otherwise we need to add it
            project.Component.Target.Add(BorzTarget);
            //and write it back
            WriteXml(customTargetsPath, project, false);
            return;
        }

        //otherwise we need to create it
        var newProject = new CustomTargets.Project()
        {
            Version = 4,
            Component = new CustomTargets.Component()
            {
                Name = "CLionExternalBuildManager",
                Target = new List<CustomTargets.Target>()
                {
                    BorzTarget
                }
            }
        };
        WriteXml(customTargetsPath, newProject, false);
    }

    private static void WriteXml<T>(string path, T tools, bool omitDeclaration = true)
    {
        var serializer = new XmlSerializer(typeof(T));
        var oldOmit = CommonSettings.OmitXmlDeclaration;

        CommonSettings.OmitXmlDeclaration = omitDeclaration;

        using var writer = new StreamWriter(path, Encoding.Default,
            new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.Read });
        //make an xml writer that doesnt write the xml declaration
        using var xmlWriter = XmlWriter.Create(writer, CommonSettings);
        serializer.Serialize(xmlWriter, tools, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));

        CommonSettings.OmitXmlDeclaration = oldOmit;
    }

    private static bool UpdateExisting(string externalToolsPath)
    {
        var serializer = new XmlSerializer(typeof(ExternalTools.ToolSet));
        var toolSet = (ExternalTools.ToolSet)serializer.Deserialize(File.OpenRead(externalToolsPath));
        if (toolSet == null) return false;

        var compileTool = toolSet.Tool.FirstOrDefault(t => t.Name == "Borz Compile");
        var cleanTool = toolSet.Tool.FirstOrDefault(t => t.Name == "Borz Clean");
        var changed = false;
        if (compileTool == null)
        {
            toolSet.Tool.Add(CompileTool);
            changed = true;
        }

        if (cleanTool == null)
        {
            toolSet.Tool.Add(CleanTool);
            changed = true;
        }

        if (!changed) return true;

        WriteXml(externalToolsPath, toolSet);

        return true;
    }
    
    public override (bool success, string error) Generate(Workspace ws, Options opt)
    {
        var ideaFolder = Path.Combine(ws.Location, ".idea");
        AssureFolder(ideaFolder);
        var tools = Path.Combine(ideaFolder, "tools");
        AssureFolder(tools);

        HandleExternalTools(tools);
        HandleCustomTargets(ideaFolder);
        return (true, String.Empty);
    }
}