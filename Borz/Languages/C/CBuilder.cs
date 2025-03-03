using System.Collections.Concurrent;
using System.Diagnostics;
using Borz.Helpers;

namespace Borz.Languages.C;

public class CBuilder : Builder
{
    
    public override (bool, string) Build(Project inproject, Options opt)
    {
        if (inproject.Language is not (Lang.C or Lang.Cpp or Lang.D))
        {
            return (false, "Unsupported language.");
        }

        var project = inproject as CProject;
        if (project == null)
        {
            return (false, "Failed to cast to CProject.");
        }

        var generateCompileCommands = BuildHelper.ShouldGenerateCompileCommands();

        CompileCommands.CompileDatabase? compileDb = null;
        var compileCmdLocation = BuildHelper.GetCompileCommandLocation(project);
        if (generateCompileCommands)
            compileDb = new CompileCommands.CompileDatabase(compileCmdLocation);

        var compiler = CompilerFactory.GetCompiler<CCompiler>(project.Language, opt);
        var isSupported = compiler.IsSupported();
        if (!isSupported.supported)
        {
            return (false, $"{compiler.Name} isn't supported: {isSupported.reason}");
        }

        compiler.GenerateSourceDependencies = true;
        compiler.CompileDatabase = compileDb;
        
        MugiLog.Debug($"Using compiler: {compiler.Name}");

        var outputDir = project.GetOutputDirectory(opt);
        var intDir = project.GetIntermediateDirectory(opt);
        
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(intDir);
        
        List<string> objects = new();
        
        var stopwatch = new Stopwatch();
        MugiLog.Info($"Compiling project: {project.Name}");

        long? compileTime = null;
        bool isPchCompiled = false;

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
                else
                {
                    var (success, pchObjDeps) = compiler.GetDependencies(project, pchObj);
                    if (success && 
                        project.GetPathsAbs(pchObjDeps)
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

            if (shouldCompilePch || opt.JustPrint)
            {
                var res = compiler.CompilePch(project);
                if (res.Exitcode != 0)
                {
                    MugiLog.Error($"Failed to compile pch for project: {project.Name}");
                    MugiLog.Error($"Compiler output:\n{res.Error}");
                    return (false, "Failed to compile PCH.");
                }

                if (!string.IsNullOrWhiteSpace(res.Error))
                {
                    MugiLog.Warning($"Compiler output:\n{res.Error}");
                }
                
                isPchCompiled = true;
            }
        }
        
        //Due to source files compiling with the pch were gonna need to recompile everything
        var sourceFilesToCompile = isPchCompiled ? 
            project.SourceFiles :
            BuildHelper.GetSourceFilesToCompile(project, compiler, ref objects);

        if (sourceFilesToCompile.Count == 0 && !isPchCompiled)
        {
            MugiLog.Debug("No files to compile.");
        }
        else
        {
            stopwatch.Restart();
            
            objects.AddRange(CompileSourceFiles(
                project, compiler, sourceFilesToCompile
                ));
            
            stopwatch.Stop();
            compileTime = stopwatch.ElapsedMilliseconds;
        }
        
        //Just a sanity check
        //lets see if the objects are bigger than 0kb
        if (objects.Count > 0 && !opt.JustPrint)
            foreach (var o in objects)
            {
                if (new FileInfo(o).Length != 0) continue;
                MugiLog.Error($"Object file is 0kb: {o}");
                return (false, $"Object file is 0kb! {o}");
            }

        var needToRelink = NeedRelink(project, opt) || isPchCompiled || opt.JustPrint;

        if (needToRelink || sourceFilesToCompile.Count != 0)
        {
            MugiLog.Debug("Linking project: " + project.Name);
            stopwatch.Restart();
            LinkProject(project, compiler, objects);
            stopwatch.Stop();
            var linkTime = stopwatch.ElapsedMilliseconds;
            
            MugiLog.Info(compileTime != null
                ? $"Compile / Link time : {compileTime}ms / {linkTime}ms"
                : $"Link time : {linkTime}ms");
            
            MugiLog.Info($"Finished {project.Name}");
            opt.SetProjectBuilt(project);
        }
        
        compileDb?.SaveToFile(compileCmdLocation);
        return (true, String.Empty);
    }

    private bool NeedRelink(CProject project, Options opt)
    {
        if (!File.Exists(project.GetOutputFilePath(opt)))
        {
            MugiLog.Debug("Binary doesn't exist, need to relink.");
            return true;
        }
        
        //CProjects and CppProjects have IsBuilt set to true when we have compiled and linked them
        //We can use this to see if we need to relink a project.
        //If our project depends on a static lib that has been built, we need to relink.
        
        var staticLibsHaveBeenBuilt = project.Dependencies.Any(dep =>
        {
            var depProj = dep as CProject;
            var isBuilt = opt.HasProjectBeenBuilt(depProj);
            if (depProj is { Type: BinType.StaticLib } && isBuilt) return true;

            return false;
        });

        if (staticLibsHaveBeenBuilt)
            return true;

        return project.Dependencies.Any(dep => opt.HasProjectBeenBuilt(dep) && dep.Type != BinType.StaticLib);
    }

    private List<string> CompileSourceFiles(CProject project, CCompiler compiler, List<string> sourceFilesToCompile)
    {
        var opt = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Borz.GetUsableThreadCount()
        };
        
        ConcurrentQueue<string> objects = new();
        var totalFiles = sourceFilesToCompile.Count;
        var objForBuild = Parallel.For(0, sourceFilesToCompile.Count, opt, i =>
        {
            var sourceFile = project.GetPathAbs(sourceFilesToCompile[i]);
            //Dont care for headers.
            if (sourceFile.EndsWith(".h")) return;
            
            MugiLog.Info($"[{i + 1}/{totalFiles}] Compiling {sourceFile}");
            
            var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";

            var objFilePath = Path.Combine(
                project.GetIntermediateDirectory(compiler.Opt),
                objFileName);

            var objFileLastWrite = BuildHelper.GetLastWriteTimeOptional(objFilePath, compiler.Opt.JustPrint);
            var sourceFileLastWrite = BuildHelper.GetLastWriteTimeOptional(sourceFile, compiler.Opt.JustPrint);

            if (!File.Exists(objFilePath) | (objFileLastWrite < sourceFileLastWrite) || compiler.Opt.JustPrint)
            {
                var result = compiler.CompileObject(project, sourceFile, objFilePath);
                if (result.Exitcode != 0)
                {
                    //Something messed up
                    var execp = new Exception("Failed to compile.\n" + result.Error);
                    MugiLog.Fatal(result.Error);
                    throw execp;
                }
                
                //Just in case
                if(result.Ouput.Length > 3)
                    MugiLog.Info(result.Ouput);
                
                if(result.Error.Length > 3)
                    MugiLog.Warning(result.Error);
            }
            
            objects.Enqueue(objFilePath);
        });
        
        if(!objForBuild.IsCompleted)
            MugiLog.Fatal("Shouldn't happen");

        return objects.ToList();
    }

    private void LinkProject(CProject project, CCompiler compiler, List<string> objects)
    {
        var result = compiler.LinkProject(project, objects.ToArray());
        if (result.Exitcode != 0)
        {
            var execp = new Exception("Failed to link.\n" + result.Error);
            MugiLog.Fatal(result.Error);
            throw execp;
        }

        //TODO: Figure out a better way than THIS:
        if (compiler.Opt.Target?.OS == "psx")
        {
            //Post process
            var objcopy = compiler.Opt.GetTarget().GetBinaryPath("objcopy", "objcopy");

            var output = project.GetOutputFilePath(compiler.Opt);
            
            List<string> cmdArgs = new();
            cmdArgs.Add("-O");
            cmdArgs.Add("binary");
            cmdArgs.Add(output);
            cmdArgs.Add(Path.Combine(project.GetOutputDirectory(compiler.Opt), Path.GetFileNameWithoutExtension(output) + ".ps-exe"));

            var ocresult = ProcUtil.RunCmdOptLog(
                objcopy,
                String.Join(' ', cmdArgs.ToArray()),
                project.Directory, compiler.Opt.JustPrint);
        }
        
        
    }
}