using AkoSharp;
using Borz.Core.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Core.Compilers;

[ShortType("Gcc")]
public class GccCompiler : ICCompiler
{
    public bool JustLog { get; set; }

    public GccCompiler()
    {
    }

    public bool GenerateSourceDependencies { get; set; }
    public bool GenerateCompileCommands { get; set; }

    public List<CppBuilder.CompileCommand> CompileCommands { get; } = new();

    public UnixUtil.RunOutput CompileObject(Project project, string sourceFile, string outputFile)
    {
        if (project is CppProject cppProject)
            return CompileObjectCpp(cppProject, sourceFile, outputFile);

        if (project is CProject cProject)
            return CompileObjectCpp(cProject, sourceFile, outputFile);

        throw new Exception("Project is not a CppProject or CProject");
    }

    private UnixUtil.RunOutput CompileObjectCpp(CProject project, string sourceFile, string outputFile)
    {
        List<string> cmdArgs = new();

        if (project.StdVersion != String.Empty)
            cmdArgs.Add($"-std=" + project.StdVersion);

        if (GenerateSourceDependencies)
            cmdArgs.Add("-MMD");

        foreach (var (key, value) in project.GetDefines())
            cmdArgs.Add(value == null ? $"-D{key}" : $"-D{key}={value}");

        var depIncludes = project.GetIncludePaths();
        cmdArgs.AddRange(depIncludes.Select(includePath => $"-I{includePath}"));

        if (project.UsePIC)
            cmdArgs.Add("-fPIC");

        if (project.StdVersion != String.Empty)
            cmdArgs.Add($"-std=" + project.StdVersion);

        foreach (var link in project.GetLibraries())
            cmdArgs.Add($"-l{link}");

        cmdArgs.AddRange(new[] { "-o", outputFile, "-c", sourceFile });

        bool useCpp = project.Language == Language.Cpp;

        string compiler = sourceFile.EndsWith(".cpp") ? "g++" : "gcc";

        if (GenerateCompileCommands)
        {
            CompileCommands.Add(
                new CppBuilder.CompileCommand()
                {
                    Directory = project.ProjectDirectory,
                    Arguments = cmdArgs.ToArray(),
                    Command = compiler,
                    File = sourceFile,
                    Output = outputFile
                });
        }

        var res = Utils.RunCmd(compiler,
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
        return res;
    }


    public UnixUtil.RunOutput LinkProject(Project unknownProject, string[] objects)
    {
        CProject project = null;
        if (unknownProject is not (CProject or CppProject))
        {
            throw new Exception("Project is not a CppProject or CProject");
        }

        project = unknownProject as CProject ?? throw new InvalidOperationException();

        string outputPath = project.OutputDirectory;

        if (project.Type == BinType.StaticLib)
        {
            string output = Path.Combine(outputPath, $"lib{project.Name}.a");
            return Utils.RunCmd("ar", $"-rcs \"{output}\" " + String.Join(" ", objects.ToArray()),
                project.ProjectDirectory, JustLog);
        }

        List<string> cmdArgs = new();

        switch (project.Type)
        {
            case BinType.SharedObj:
                outputPath = Path.Combine(outputPath, $"lib{project.Name}.so");
                break;
            case BinType.StaticLib:
                outputPath = Path.Combine(outputPath, $"lib{project.Name}.a");
                break;
            case BinType.WindowsApp:
                outputPath = Path.Combine(outputPath, $"{project.Name}.exe");
                break;
            default:
                outputPath = Path.Combine(outputPath, project.Name);
                break;
        }

        cmdArgs.Add("-o");
        cmdArgs.Add(outputPath);

        cmdArgs.AddRange(objects);

        foreach (var libraryPath in project.GetLibraryPaths())
            cmdArgs.Add($"-L{libraryPath}");

        foreach (var link in project.GetLibraries())
            cmdArgs.Add($"-l{link}");

        foreach (var rpath in project.GetRPaths(unknownProject.GetPathAbs(unknownProject.OutputDirectory)))
            cmdArgs.Add($"-Wl,-rpath={rpath}");

        switch (project.Type)
        {
            case BinType.SharedObj:
                cmdArgs.Add("-shared");
                break;
            case BinType.StaticLib:
                cmdArgs.Add("-static");
                break;
        }


        if (Borz.UseMold)
        {
            cmdArgs.Add("-fuse-ld=mold");
        }

        var res = Utils.RunCmd(project.Language == Language.C ? "gcc" : "g++",
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
        return res;
    }

    public bool GetDependencies(Project project, string objectFile, out string[] dependencies)
    {
        //find dependency file for source file
        //this will be the source files name with .d appended located in the projects intermediate directory
        var depFile = Path.Combine(project.IntermediateDirectory, Path.GetFileNameWithoutExtension(objectFile) + ".d");

        dependencies = Array.Empty<string>();

        if (!File.Exists(depFile))
            return false;

        var dep = PosixDepParser.Parse(File.ReadAllText(depFile));
        var objFileAbs = Path.Combine(project.IntermediateDirectory, objectFile);
        if (!dep.ContainsKey(objFileAbs)) return false;
        dependencies = dep[objFileAbs].ToArray();
        return true;
    }

    public bool IsSupported(out string reason)
    {
        //Make sure gcc is installed
        var res = Utils.RunCmd("gcc", "--version");
        if (res.Exitcode != 0)
        {
            reason = "GCC is not installed.";
            return false;
        }

        /*
         * Example output from gcc --version:
         * gcc (GCC) 12.2.1 20221121 (Red Hat 12.2.1-4)
         * ...
         */
        //Get the first line and split it by spaces
        var split = res.Ouput.Split('\n')[0].Split(' ');
        //Get the version number
        var version = split[2];
        //Check if the version is 10 or higher
        var versionParts = version.Split('.');
        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(versionParts[1]);
        var patch = int.Parse(versionParts[2]);
        if (major >= 10)
        {
            reason = "";
            return true;
        }

        reason = "GCC version is too old. Please install GCC 10 or higher.";
        return false;
    }

    public string GetFriendlyName(bool asLinker)
    {
        if (Borz.UseMold && asLinker)
        {
            return GetFriendlyName() + " w/ Mold";
        }

        return GetFriendlyName();
    }

    public void SetJustLog(bool justLog)
    {
        JustLog = justLog;
    }

    public string GetFriendlyName()
    {
        return "GCC";
    }
}