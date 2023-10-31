using System.Collections.Concurrent;
using System.Diagnostics;
using Borz.Core.Helpers;

namespace Borz.Core.Languages.C;

public class CppBuilder : IBuilder
{
    public bool Simulate { get; set; } = false;

    public static string[] SupportedLangs = new[]
    {
        Language.C,
        Language.Cpp,
        Language.D
    };

    public bool Build(Project inProj, bool simulate)
    {
        var language = inProj.Language;
        if (!SupportedLangs.Contains(language))
            throw new Exception($"Language '{language}' is not supported by CppBuilder");

        var project = (CProject)inProj;

        Simulate = simulate;

        var generateCompileCommands = BuildHelper.ShouldGenerateCompileCommands(language);

        CompileCommands.CompileDatabase? compileDb = null;
        var compileCmdLocation = BuildHelper.GetCompileCommandLocation(language, project);
        if (generateCompileCommands)
            compileDb = new CompileCommands.CompileDatabase(compileCmdLocation);

        var compiler = BuildHelper.CreateCompiler<ICCompiler>(language);
        var linker = BuildHelper.CreateLinker<ICCompiler>(language);

        if (!BuildHelper.ValidateCCompiler(compiler) || !BuildHelper.ValidateCCompiler(linker, true))
            throw new Exception("Compiler or linker is invalid");

        compiler.GenerateSourceDependencies = true;
        compiler.CompileDatabase = compileDb;
        compiler.OnlyOutputCompileCommands = Simulate;

        compiler.JustLog = Simulate;
        linker.JustLog = Simulate;

        MugiLog.Debug($"Using compiler: {compiler.GetFriendlyName()}");
        MugiLog.Debug($"Using linker: {linker.GetFriendlyName(true)}");

        var outputDir = project.OutputDirectory;
        var intDir = project.IntermediateDirectory;

        Utils.CheckFolder(outputDir);
        Utils.CheckFolder(intDir);

        List<string> objects = new();

        var stopwatch = new Stopwatch();
        MugiLog.Info("Compiling project: " + project.Name);

        long? compileTime = null;

        var pchCompiled = false;

        if (!string.IsNullOrWhiteSpace(project.PchHeader))
        {
            //deal with pch
            //some compiler agnostic way to handle pch
            var pchObj = compiler.GetCompiledPchLocation(project);
            var shouldCompilePch = false;
            if (File.Exists(pchObj))
            {
                if (File.GetLastWriteTimeUtc(pchObj) < File.GetLastWriteTimeUtc(project.GetPathAbs(project.PchHeader)))
                {
                    MugiLog.Debug($"Pch file is out of date for project(file itself out of date): {project.Name}");
                    shouldCompilePch = true;
                }
                else if (compiler.GetDependencies(project, pchObj, out var pchObjDeps))
                {
                    if (project.GetPathsAbs(pchObjDeps)
                        .Any(dep => File.GetLastWriteTimeUtc(pchObj) < File.GetLastWriteTimeUtc(dep)))
                    {
                        MugiLog.Debug($"Pch file is out of date for project(dep out of date): {project.Name}");
                        shouldCompilePch = true;
                    }
                }
            }
            else
            {
                shouldCompilePch = true;
            }

            if (shouldCompilePch || Simulate)
            {
                var res = compiler.CompilePch(project);
                if (res.Exitcode != 0)
                {
                    MugiLog.Error($"Failed to compile pch for project: {project.Name}");
                    MugiLog.Error($"Compiler output:\n{res.Error}");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(res.Error))
                    //Mostly warnings 
                    MugiLog.Warning($"Compiler output:\n{res.Error}");

                pchCompiled = true;
            }
        }

        //Due to source files compiling with this, were gonna need to recompile everything
        var sourceFilesToCompile = GetSourceFilesToCompile(project, compiler, ref objects, !pchCompiled);

        if (sourceFilesToCompile.Count == 0 && !pchCompiled)
        {
            Borz.BuildLog.Enqueue("Was gonna compile, but no files to compile for project: " + project.Name);
            MugiLog.Debug("No files to compile.");
        }
        else
        {
            Borz.BuildLog.Enqueue($"Compiling source files for project: {project.Name}");
            var totalFiles = (uint)project.SourceFiles.Count;
            uint currentFile = 1;
            stopwatch.Restart();

            objects.AddRange(CompileSourceFiles(
                project, compiler, sourceFilesToCompile
            ));

            stopwatch.Stop();
            compileTime = stopwatch.ElapsedMilliseconds;
        }

        //Just a sanity check
        //lets see if the objects are bigger than 0kb
        if (objects.Count > 0 && !Simulate)
            foreach (var o in objects)
            {
                if (new FileInfo(o).Length != 0) continue;
                MugiLog.Error($"Object file is 0kb: {o}");
                return false;
            }

        var needToRelink = NeedRelink(project) || pchCompiled || Simulate;

        if (needToRelink || sourceFilesToCompile.Count != 0)
        {
            Borz.BuildLog.Enqueue($"Linking project: {project.Name}");
            MugiLog.Debug("Linking project: " + project.Name);
            stopwatch.Restart();
            LinkProject(project, linker, objects);
            stopwatch.Stop();
            var linkTime = stopwatch.ElapsedMilliseconds;

            MugiLog.Info(compileTime != null
                ? $"Compile / Link time : {compileTime}ms / {linkTime}ms"
                : $"Link time : {linkTime}ms");

            MugiLog.Info($"Finished {project.Name}");

            project.IsBuilt = true;
        }

        compileDb?.SaveToFile(compileCmdLocation);
        project.CallFinishedCompiling();

        return true;
    }


    private List<string> GetSourceFilesToCompile(CProject project, ICCompiler compiler, ref List<string> objects,
        bool checkDeps = true)
    {
        return BuildHelper.GetSourceFilesToCompile(project, project.SourceFiles, compiler, ref objects, checkDeps,
            Simulate, ".o");
    }

    private bool NeedRelink(CProject project)
    {
        if (!project.OutputFileExists())
        {
            MugiLog.Debug("Binary doesn't exist, need to relink.");
            return true;
        }

        //CProjecst and CppProjects have IsBuilt set to true when we have compiled and linked them
        //We can use this to see if we need to relink a project.
        //If our project depends on a static lib that has been built, we need to relink.

        var staticLibsHaveBeenBuilt = project.Dependencies.Any(dep =>
        {
            var depProj = dep as CProject;
            if (depProj is { IsBuilt: true, Type: BinType.StaticLib }) return true;

            return false;
        });

        if (staticLibsHaveBeenBuilt)
            return true;

        return project.Dependencies.Any(dep =>
        {
            var depProj = dep as CProject;
            if (depProj is null)
                return false;
            return depProj.IsBuilt && depProj.Type != BinType.StaticLib;
        });
    }

    private List<string> CompileSourceFiles(CProject project, ICCompiler compiler, List<string> sourceFilesToCompile)
    {
        ConcurrentQueue<string> objects = new();
        var totalFiles = sourceFilesToCompile.Count;
        var objForBuild = Parallel.For(0, sourceFilesToCompile.Count, Borz.ParallelOptions, i =>
        {
            var sourceFile = project.GetPathAbs(sourceFilesToCompile[i]);
            //Dont care for headers.
            if (sourceFile.EndsWith(".h")) return;

            MugiLog.Info($"[{i + 1}/{totalFiles}] Compiling {sourceFile}");

            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";

            var objFilePath = Path.Combine(
                project.IntermediateDirectory,
                objFileName);

            var objFileLastWrite = BuildHelper.GetLastWriteTimeOptional(objFilePath, Simulate);
            var sourceFileLastWrite = BuildHelper.GetLastWriteTimeOptional(sourceFile, Simulate);


            if (!File.Exists(objFilePath) | (objFileLastWrite < sourceFileLastWrite) || Simulate)
            {
                var sourceAbsolute = Path.Combine(project.ProjectDirectory, sourceFile);
                var result = compiler.CompileObject(project, sourceFile, objFilePath);
                if (result.Exitcode != 0)
                {
                    //Something messed up
                    var execp = new Exception("Failed to compile.\n" + result.Error);
                    MugiLog.Fatal(result.Error);
                    throw execp;
                }

                //log info just in case
                if (result.Ouput.Length > 3)
                    MugiLog.Info(result.Ouput);

                if (result.Error.Length > 3)
                    //Some warning
                    MugiLog.Warning(result.Error);
            }

            objects.Enqueue(objFilePath);
        });

        if (!objForBuild.IsCompleted)
            MugiLog.Fatal("Shouldn't happen");
        return objects.ToList();
    }

    private void LinkProject(CProject project, ICCompiler linker, List<string> objects)
    {
        var result = linker.LinkProject(project, objects.ToArray());
        if (result.Exitcode != 0)
        {
            var execp = new Exception("Failed to link.\n" + result.Error);
            MugiLog.Fatal(result.Error);
            throw execp;
        }
    }
}