using AkoSharp;
using Borz.Languages.C;
using MoonSharp.Interpreter;

namespace Borz.Helpers;

public class BuildHelper
{
    [MoonSharpHidden]
    public static bool ShouldGenerateCompileCommands()
    {
        var avar = Borz.Config.Get("builder", "compileCmds");
        if (avar is { Type: AkoVar.VarType.BOOL } && avar.Value == true)
            return true;
        return false;
    }

    [MoonSharpHidden]
    public static bool ShouldCombineCommands()
    {
        var avar = Borz.Config.Get("builder", "combineCmds");
        if (avar is { Type: AkoVar.VarType.BOOL } && avar.Value == true)
            return true;
        return false;
    }
    
    public static string GetCompileCommandLocation(Project project)
    {
        var doWorkspaceCompileCommands = ShouldCombineCommands();
        if (project.Owner != null && doWorkspaceCompileCommands)
        {
            //Wants workspace cc and it has a valid owner, nice
            return Path.Combine(project.Owner.Location, "compile_commands.json");
        }

        return Path.Combine(project.Directory, "compile_commands.json");
    }


    public static List<string> GetSourceFilesToCompile(CProject project, CCompiler compiler, ref List<string> objects)
    {
        if (compiler.Opt.JustPrint)
            return project.SourceFiles;

        List<string> sourceFilesToCompile = new();

        foreach (var sourceFile in project.SourceFiles)
        {
            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + compiler.ObjectFileExtension;
            var objFileAbs = Path.Combine(project.GetIntermediateDirectory(compiler.Opt), objFileName);

            if (!File.Exists(objFileAbs))
            {
                //No object for source, compile it
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
            var deps = compiler.GetDependencies(project, objFileName);
            if (deps.success)
            {
                foreach (var dep in deps.dependencies)
                {
                    var depLastWrite = File.GetLastWriteTime(dep);
                    if (depLastWrite > objFileLastWrite)
                    {
                        //Dependency is newer, compile it.
                        needsCompile = true;
                        break;
                    }
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
    
    public static DateTime GetLastWriteTimeOptional(string path, bool justLog = false)
    {
        return justLog ? DateTime.Now : File.GetLastWriteTime(path);
    }
}