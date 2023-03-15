using AkoSharp;
using Borz.Languages.C;
using Borz.PkgConfig;
using Microsoft.VisualBasic;

namespace Borz.Compilers;

[ShortType("Gcc")]
public class GccCompiler : ICCompiler
{
    public bool JustLog { get; set; }
    protected bool UseMold { get; set; }

    public GccCompiler()
    {
    }

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

        foreach (var (key, value) in project.Defines)
            cmdArgs.Add(value == null ? $"-D{key}" : $"-D{key}={value}");

        foreach (var projectDependency in project.Dependencies)
        {
            if (projectDependency is CppProject cppProject)
            {
                var includePath = cppProject.GetPathsAbs(cppProject.PublicIncludePaths.ToArray());
                cmdArgs.AddRange(includePath.Select(s => $"-I{s}"));
            }
            else if (projectDependency is CProject cProject)
            {
                var includePath = cProject.GetPathsAbs(cProject.PublicIncludePaths.ToArray());
                cmdArgs.AddRange(includePath.Select(s => $"-I{s}"));
            }
            else if (projectDependency is PkgConfigProject pkg)
            {
                //This is a pkgconfig project, so we need to add the include paths, pkgconifg projects always are absolute
                cmdArgs.AddRange(pkg.IncludePaths.Select(s => $"-I{s}"));
                //also defines
                foreach (var (key, value) in pkg.Defines)
                    cmdArgs.Add(value == null ? $"-D{key}" : $"-D{key}={value}");
            }
            else
            {
                MugiLog.Error(
                    $"GCC: Unknown dependency type {projectDependency.GetType().Name} for {projectDependency.Name}");
            }
        }

        foreach (var includePath in project.PrivateIncludePaths)
            cmdArgs.Add($"-I{includePath}");

        foreach (var includePath in project.PublicIncludePaths)
            cmdArgs.Add($"-I{includePath}");

        if (project.UsePIC)
            cmdArgs.Add("-fPIC");

        if (project.StdVersion != String.Empty)
            cmdArgs.Add($"-std=" + project.StdVersion);

        foreach (var link in project.Links)
            cmdArgs.Add($"-l{link}");

        cmdArgs.AddRange(new[] { "-o", outputFile, "-c", sourceFile });

        bool useCpp = project.Language == Language.Cpp;

        string compiler = sourceFile.EndsWith(".cpp") ? "g++" : "gcc";

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

        foreach (var libraryPath in project.LibraryPaths)
        {
            cmdArgs.Add($"-L{libraryPath}");
        }

        List<string> rpaths = new();

        //setup library paths for dependencies
        foreach (Project projectDependency in project.Dependencies)
        {
            if (projectDependency is CppProject cppProject)
            {
                var libraryPath = cppProject.GetPathAbs(cppProject.OutputDirectory);
                cmdArgs.Add($"-L{libraryPath}");
                //need to make sure rpath is set for the library
                //Figure out the relative path from the output directory to the library
                var relativePath = Path.GetRelativePath(project.GetPathAbs(project.OutputDirectory), libraryPath);
                rpaths.Add(relativePath);
            }
            else if (projectDependency is CProject cProject)
            {
                var libraryPath = cProject.GetPathAbs(cProject.OutputDirectory);
                cmdArgs.Add($"-L{libraryPath}");
                var relativePath = Path.GetRelativePath(project.GetPathAbs(project.OutputDirectory), libraryPath);
                rpaths.Add(relativePath);
            }
            else if (projectDependency is PkgConfigProject pkg)
            {
                //This is a pkgconfig project, so we need to add the library paths, pkgconifg projects always are absolute
                cmdArgs.AddRange(pkg.LibraryPaths.Select(s => $"-L{s}"));
            }
        }

        foreach (var link in project.Links)
        {
            cmdArgs.Add($"-l{link}");
        }

        foreach (var rpath in rpaths)
        {
            cmdArgs.Add($"-Wl,-rpath={rpath}");
        }

        foreach (Project projectDependency in project.Dependencies)
        {
            if (projectDependency is CppProject cppProject)
            {
                cmdArgs.Add("-l" + cppProject.Name);
            }
            else if (projectDependency is CProject cProject)
            {
                cmdArgs.Add("-l" + cProject.Name);
            }
            else if (projectDependency is PkgConfigProject pkg)
            {
                cmdArgs.AddRange(pkg.Libraries.Select(s => $"-l{s}"));
            }
        }

        switch (project.Type)
        {
            case BinType.SharedObj:
                cmdArgs.Add("-shared");
                break;
            case BinType.StaticLib:
                cmdArgs.Add("-static");
                break;
        }

        if (UseMold)
        {
            cmdArgs.Add("-fuse-ld=mold");
        }

        var res = Utils.RunCmd(project.Language == Language.C ? "gcc" : "g++",
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
        return res;
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

    public void SetJustLog(bool justLog)
    {
        JustLog = justLog;
    }
}