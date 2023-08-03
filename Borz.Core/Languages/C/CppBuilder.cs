using System.Collections.Concurrent;
using System.Diagnostics;
using AkoSharp;
using Newtonsoft.Json;

namespace Borz.Core.Languages.C;

public class CppBuilder : IBuilder
{
    public bool Simulate { get; set; } = false;

    private static void CheckFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public bool Build(Project inProj, bool simulate)
    {
        var project = (CProject)inProj;
        //make sure inProj is a CppProject or CProject
        if (!(inProj is CppProject || inProj is CProject))
            throw new Exception("Project is not a CppProject or CProject");

        //Just logging is just simulating a build
        if (simulate)
            Simulate = true;

        var generateCompileCommands = ShouldGenerateCompileCommands();
        //see if we should combind this into the workspace compile_commands.json
        bool doWorkspaceCompileCommands = Borz.Config.Get("builder", "cpp", "combineCmds");

        var compiler = CreateCompiler();
        var linker = CreateLinker();

        if (!ValidateCompiler(compiler) || !ValidateLinker(linker))
            throw new Exception("Compiler or linker is invalid");

        compiler.GenerateSourceDependencies = true;
        compiler.GenerateCompileCommands = generateCompileCommands;
        compiler.OnlyOutputCompileCommands = Simulate;

        compiler.SetJustLog(Simulate);
        linker.SetJustLog(Simulate);

        MugiLog.Debug($"Using compiler: {compiler.GetFriendlyName()}");
        MugiLog.Debug($"Using linker: {linker.GetFriendlyName(true)}");

        var outputDir = project.OutputDirectory;
        var intDir = project.IntermediateDirectory;

        CheckFolder(outputDir);
        CheckFolder(intDir);

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
        List<string> sourceFilesToCompile = GetSourceFilesToCompile(project, compiler, ref objects, !pchCompiled);

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
            if (generateCompileCommands)
                WriteCompileCommands(
                    doWorkspaceCompileCommands ? Workspace.Location : project.ProjectDirectory,
                    compiler.CompileCommands.ToArray());
        }

        var needToRelink = NeedRelink(project) || pchCompiled || Simulate;
        if (!needToRelink)
            //If the binary doesn't exist, we need to link it
            if (!File.Exists(project.GetOutputFilePath()))
            {
                MugiLog.Debug("Binary doesn't exist, need to relink.");
                needToRelink = true;
            }

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

        project.CallFinishedCompiling();

        return true;
    }

    private List<string> GetSourceFilesToCompile(CProject project, ICCompiler compiler, ref List<string> objects,
        bool checkDeps = true)
    {
        if (Simulate)
            //If we're simulating, we need to compile everything
            return project.SourceFiles;

        List<string> sourceFilesToCompile = new();

        foreach (var sourceFile in project.SourceFiles)
        {
            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";
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


    public void WriteCompileCommands(string outputLocation, CompileCommand[] commands)
    {
        //Write compile_commands.json
        //But if it already exists, find files that are in our list and update them.
        //This is to avoid recompiling everything when we add a new file.

        List<CompileCommand> jsonCommands = new();

        var compileCommandsPath = Path.Combine(outputLocation, "compile_commands.json");
        if (File.Exists(compileCommandsPath))
        {
            var json = JsonConvert.DeserializeObject<List<CompileCommand>>(File.ReadAllText(compileCommandsPath));
            if (json != null)
            {
                jsonCommands = json;
                //remove all commands that are in our list.
                foreach (var command in commands) jsonCommands.RemoveAll(c => c.File == command.File);
            }
        }

        jsonCommands.AddRange(commands);
        var jsonStr = JsonConvert.SerializeObject(jsonCommands, Formatting.Indented);
        File.WriteAllText(compileCommandsPath, jsonStr);
    }

    private bool ShouldGenerateCompileCommands()
    {
        var akoVarComCmds = Borz.Config.Get("builder", "cpp", "compileCmds");
        if (akoVarComCmds is { Type: AkoVar.VarType.BOOL } && akoVarComCmds.Value == true)
            return true;
        return false;
    }

    private ICCompiler CreateCompiler()
    {
        Type compilerType = Borz.Config.Get("compiler", "cxx");
        return compilerType.CreateInstance<ICCompiler>()!;
    }

    private ICCompiler CreateLinker()
    {
        Type linkerType = Borz.Config.Get("linker", "cxx");
        return linkerType.CreateInstance<ICCompiler>()!;
    }

    private bool ValidateCompiler(ICCompiler compiler)
    {
        if (!compiler.IsSupported(out var reason))
        {
            MugiLog.Fatal("Compiler not supported: " + reason);
            return false;
        }

        return true;
    }

    private bool ValidateLinker(ICCompiler linker)
    {
        if (!linker.IsSupported(out var reason))
        {
            MugiLog.Fatal("Linker not supported: " + reason);
            return false;
        }

        return true;
    }

    private bool NeedRelink(CProject project)
    {
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

            var objFileLastWrite = File.GetLastWriteTime(objFilePath);
            var sourceFileLastWrite = File.GetLastWriteTime(sourceFile);


            if (!File.Exists(objFilePath) | (objFileLastWrite < sourceFileLastWrite))
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

    //Either arguments or command is required.
    //arguments is preferred, as shell (un)escaping is a possible source of errors.
    [Serializable]
    public class CompileCommand
    {
        //The working directory of the compilation.
        //All paths specified in the command or file fields must be either absolute or relative to this directory.
        [JsonProperty("directory")] public string Directory { get; set; } = "";

        //The main translation unit source processed by this compilation step.
        //This is used by tools as the key into the compilation database.
        //There can be multiple command objects for the same file,
        //for example if the same source file is compiled with different configurations.
        [JsonProperty("file")] public string File { get; set; } = "";

        //The compile command argv as list of strings.
        //This should run the compilation step for the translation unit file.
        //arguments[0] should be the executable name, such as clang++.
        //Arguments should not be escaped, but ready to pass to execvp().
        [JsonProperty("arguments")] public string[] Arguments { get; set; } = Array.Empty<string>();

        //The compile command as a single shell-escaped string.
        //Arguments may be shell quoted and escaped following platform conventions,
        //with ‘"’ and ‘\’ being the only special characters.
        //Shell expansion is not supported.
        [JsonProperty("command")] public string Command { get; set; } = "";

        //The name of the output created by this compilation step.
        //This field is optional.
        //It can be used to distinguish different processing modes of the same input file.
        [JsonProperty("output")] public string Output { get; set; } = "";
    }
}