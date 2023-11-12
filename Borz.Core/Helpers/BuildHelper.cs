using AkoSharp;
using Borz.Core.Languages.C;

namespace Borz.Core.Helpers;

public class BuildHelper
{
    public static List<string> GetSourceFilesToCompile(Project project, List<string> sourceFiles, ICCompiler compiler,
        ref List<string> objects,
        bool checkDeps = true, bool simulate = false, string objectExtension = ".o")
    {
        if (simulate)
            return sourceFiles;

        List<string> sourceFilesToCompile = new();

        foreach (var sourceFile in sourceFiles)
        {
            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + objectExtension;
            var objFileAbs = Path.Combine(project.IntermediateDirectory, objFileName);

            if (!checkDeps)
            {
                sourceFilesToCompile.Add(sourceFile);
                continue;
            }


            if (!File.Exists(objFileAbs))
            {
                //No object for source, compile it.
                sourceFilesToCompile.Add(sourceFile);
                MugiLog.Debug("No object file for source file recompiling: " + sourceFile);
                continue;
            }

            var sourceFileLastWrite = File.GetLastWriteTime(project.GetPathAbs(sourceFile));
            var objFileLastWrite = File.GetLastWriteTime(objFileAbs);

            //See if source file is newer than object file.
            if (sourceFileLastWrite > objFileLastWrite)
            {
                //Source file is newer, compile it.
                sourceFilesToCompile.Add(sourceFile);
                MugiLog.Debug("Source file is newer than object file recompiling: " + sourceFile);
                continue;
            }

            var needsCompile = false;

            if (compiler.GetDependencies(project, objFileName, out var deps))
                //See if any of the dependencies are newer than the object file.
                foreach (var dep in deps)
                {
                    var depLastWrite = File.GetLastWriteTime(dep);
                    if (depLastWrite > objFileLastWrite)
                    {
                        //Dependency is newer, compile it.
                        needsCompile = true;
                        break;
                    }
                }

            if (needsCompile)
            {
                sourceFilesToCompile.Add(sourceFile);
                MugiLog.Debug("Dependency is newer than object file recompiling: " + sourceFile);
            }
            else
            {
                objects.Add(objFileAbs);
                MugiLog.Debug("Object file is up to date: " + sourceFile);
            }
        }

        return sourceFilesToCompile;
    }

    public static bool ShouldGenerateCompileCommands(string language)
    {
        var avar = Borz.Config.Get("builder", language, "compileCmds");
        if (avar is { Type: AkoVar.VarType.BOOL } && avar.Value == true)
            return true;
        return false;
    }

    public static bool ShouldCombineCommands(string language)
    {
        var avar = Borz.Config.Get("builder", language, "combineCmds");
        if (avar is { Type: AkoVar.VarType.BOOL } && avar.Value == true)
            return true;
        return false;
    }

    public static string GetCompileCommandLocation(string language, Project project)
    {
        var doWorkspaceCompileCommands = ShouldCombineCommands(language);
        return Path.Combine(doWorkspaceCompileCommands ? Workspace.Location : project.ProjectDirectory,
            "compile_commands.json");
    }

    public static T CreateCompiler<T>(string language)
    {
        Type compilerType = Borz.Config.Get("compiler", language);
        return compilerType.CreateInstance<T>()!;
    }

    public static T CreateLinker<T>(string language)
    {
        Type compilerType = Borz.Config.Get("linker", language);
        return compilerType.CreateInstance<T>()!;
    }

    public static bool ValidateCCompiler(ICCompiler compiler, bool isLinker = false)
    {
        if (compiler.IsSupported(out var reason)) return true;

        MugiLog.Fatal((isLinker ? "Linker" : "Compiler") + " not supported: " + reason);
        return false;
    }

    public static DateTime GetLastWriteTimeOptional(string path, bool isSimulating = false)
    {
        return isSimulating ? DateTime.Now : File.GetLastWriteTime(path);
    }
}