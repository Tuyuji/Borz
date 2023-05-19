using System.Diagnostics;
using AkoSharp;
using Newtonsoft.Json;

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
        if (!(inProj is CppProject || inProj is CProject))
        {
            throw new Exception("Project is not a CppProject or CProject");
        }

        bool generateCompileCommands = ShouldGenerateCompileCommands();

        var compiler = CreateCompiler();
        var linker = CreateLinker();

        if (!ValidateCompiler(compiler) || !ValidateLinker(linker))
            return false;

        compiler.GenerateSourceDependencies = true;
        compiler.GenerateCompileCommands = generateCompileCommands;

        compiler.SetJustLog(justLog);
        linker.SetJustLog(justLog);

        MugiLog.Info($"Using compiler: {compiler.GetFriendlyName()}");
        MugiLog.Info($"Using linker: {linker.GetFriendlyName(true)}");

        string outputDir = project.OutputDirectory;
        string intDir = project.IntermediateDirectory;

        CheckFolder(outputDir);
        CheckFolder(intDir);

        List<string> objects = new List<string>();

        Stopwatch stopwatch = new Stopwatch();
        MugiLog.Info("Compiling project: " + project.Name);

        List<string> sourceFilesToCompile = GetSourceFilesToCompile(project, compiler, ref objects);

        long? compileTime = null;

        bool pchCompiled = false;

        if (!string.IsNullOrWhiteSpace(project.PchHeader))
        {
            //deal with pch
            //some compiler agnostic way to handle pch
            var pchObj = compiler.GetCompiledPchLocation(project);
            bool shouldCompilePch = false;
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

            if (shouldCompilePch)
            {
                var res = compiler.CompilePch(project);
                if (res.Exitcode != 0)
                {
                    MugiLog.Error($"Failed to compile pch for project: {project.Name}");
                    MugiLog.Error($"Compiler output: {res.Ouput}");
                    return false;
                }

                pchCompiled = true;
            }
        }

        if (sourceFilesToCompile.Count == 0)
        {
            Borz.BuildLog.Enqueue("Was gonna compile, but no files to compile for project: " + project.Name);
            MugiLog.Info("No files to compile.");
        }
        else
        {
            Borz.BuildLog.Enqueue($"Compiling source files for project: {project.Name}");
            uint totalFiles = (uint)project.SourceFiles.Count;
            uint currentFile = 1;
            stopwatch.Restart();

            objects.AddRange(CompileSourceFiles(
                project, compiler, sourceFilesToCompile
            ));

            stopwatch.Stop();
            compileTime = stopwatch.ElapsedMilliseconds;
            if (generateCompileCommands)
                WriteCompileCommands(project.ProjectDirectory, compiler.CompileCommands.ToArray());
        }

        bool needToRelink = NeedRelink(project) || pchCompiled;

        if (needToRelink || sourceFilesToCompile.Count != 0)
        {
            Borz.BuildLog.Enqueue($"Linking project: {project.Name}");
            MugiLog.Info("Linking project: " + project.Name);
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

    private List<string> GetSourceFilesToCompile(CProject project, ICCompiler compiler, ref List<string> objects)
    {
        List<string> sourceFilesToCompile = new List<string>();

        foreach (var sourceFile in project.SourceFiles)
        {
            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";
            var objFileAbs = Path.Combine(project.IntermediateDirectory, objFileName);
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

            bool needsCompile = false;

            if (compiler.GetDependencies(project, objFileName, out var deps))
            {
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

        List<CompileCommand> jsonCommands = new List<CompileCommand>();

        var compileCommandsPath = Path.Combine(outputLocation, "compile_commands.json");
        if (File.Exists(compileCommandsPath))
        {
            var json = JsonConvert.DeserializeObject<List<CompileCommand>>(File.ReadAllText(compileCommandsPath));
            if (json != null)
            {
                jsonCommands = json;
                //remove all commands that are in our list.
                foreach (var command in commands)
                {
                    jsonCommands.RemoveAll(c => c.File == command.File);
                }
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
        return project.Dependencies.Any(dep =>
        {
            CProject? depProj = dep as CProject;
            return depProj is { IsBuilt: true, Type: BinType.StaticLib }; //
        });
    }

    private List<string> CompileSourceFiles(CProject project, ICCompiler compiler, List<string> sourceFilesToCompile)
    {
        List<string> objects = new List<string>();
        int totalFiles = sourceFilesToCompile.Count;
        var objForBuild = Parallel.For(0, sourceFilesToCompile.Count, Borz.ParallelOptions, i =>
        {
            var sourceFile = project.GetPathAbs(sourceFilesToCompile[i]);
            //Dont care for headers.
            if (sourceFile.EndsWith(".h"))
            {
                return;
            }

            MugiLog.Info($"[{i + 1}/{totalFiles}] Compiling {sourceFile}");

            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";

            string objFilePath = Path.Combine(
                project.IntermediateDirectory,
                objFileName);

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
        return objects;
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