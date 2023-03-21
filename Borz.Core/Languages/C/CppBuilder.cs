using System.Diagnostics;

namespace Borz.Core.Languages.C;

public class CppBuilder : IBuilder
{
    private static void CheckFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public bool Build(Project inProj, bool justLog)
    {
        CProject project = (CProject)inProj;
        //make sure inProj is a CppProject or CProject
        if (inProj is not CppProject)
        {
            if (inProj is not CProject)
            {
                throw new Exception("Project is not a CppProject or CProject");
            }
        }

        Type compilerType = Borz.Config.Get("compiler", "cxx");
        Type linkerType = Borz.Config.Get("linker", "cxx");

        var compiler = compilerType.CreateInstance<ICCompiler>()!;
        var linker = linkerType.CreateInstance<ICCompiler>()!;

        string reason = "";
        if (!compiler.IsSupported(out reason))
        {
            MugiLog.Error($"Can't use compiler {compilerType.Name} because: {reason}");
            return false;
        }

        if (!linker.IsSupported(out reason))
        {
            MugiLog.Error($"Can't use linker {linkerType.Name} because: {reason}");
            return false;
        }

        compiler.SetJustLog(justLog);
        linker.SetJustLog(justLog);

        MugiLog.Info($"Using compiler: {compilerType.Name}");
        MugiLog.Info($"Using linker: {linkerType.Name}");

        string outputDir = project.OutputDirectory;
        string intDir = project.IntermediateDirectory;

        CheckFolder(outputDir);
        CheckFolder(intDir);
        List<string> objects = new List<string>();


        Stopwatch stopwatch = new Stopwatch();
        MugiLog.Info("Compiling project: " + project.Name);
        uint totalFiles = (uint)project.SourceFiles.Count;
        uint currentFile = 0;
        stopwatch.Restart();
        var objForBuild = Parallel.For(0, project.SourceFiles.Count, Borz.ParallelOptions, i =>
        {
            var sourceFile = project.GetPathAbs(project.SourceFiles[i]);
            //Dont care for headers.
            if (sourceFile.EndsWith(".h"))
            {
                Interlocked.Increment(ref currentFile);
                return;
            }

            MugiLog.Info($"[{i}/{totalFiles}] Compiling {sourceFile}");

            string objFilePath = Path.Combine(
                project.IntermediateDirectory,
                Path.GetFileNameWithoutExtension(sourceFile) + ".o");

            var objFileLastWrite = File.GetLastWriteTime(objFilePath);
            var sourceFileLastWrite = File.GetLastWriteTime(sourceFile);


            if (!File.Exists(objFilePath) | objFileLastWrite < sourceFileLastWrite)
            {
                string sourceAbsolute = Path.Combine(project.ProjectDirectory, sourceFile);
                var result = compiler.CompileObject(project, sourceFile, objFilePath);
                if (result.Exitcode != 0)
                {
                    //Something fucked up
                    var execp = new Exception("Failed to compile.\n" + result.Error);
                    MugiLog.Fatal(result.Error);
                    throw execp;
                }
            }

            objects.Add(objFilePath);
        });

        if (!objForBuild.IsCompleted)
            MugiLog.Fatal("Shouldn't happen");

        stopwatch.Stop();
        var compileTime = stopwatch.ElapsedMilliseconds;

        MugiLog.Info("Linking project: " + project.Name);
        stopwatch.Restart();
        var result = linker.LinkProject(project, objects.ToArray());
        if (result.Exitcode != 0)
        {
            var execp = new Exception("Failed to link.\n" + result.Error);
            MugiLog.Fatal(result.Error);
            throw execp;
        }

        stopwatch.Stop();
        var linkTime = stopwatch.ElapsedMilliseconds;

        MugiLog.Info($"Compile / Link time : {compileTime}ms / {linkTime}ms");
        MugiLog.Info($"Finished {project.Name}");

        project.CallFinishedCompiling();

        return true;
    }
}