using System.Runtime.InteropServices;
using Borz.Helpers;
using Borz.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Compilers;

public abstract class CommonUnixCCompiler : CCompiler
{
    public string CCompilerElf = "";
    public string CppCompilerElf = "";
    public string PchExt = ".gch";

    protected CommonUnixCCompiler(Options opt) : base(opt)
    {
    }

    public override ProcUtil.RunOutput CompileObject(Project inPrj, string sourceFile, string outputFile)
    {
        if(inPrj is not CProject project)
            throw new Exception("Project is not a CppProject or CProject");
        
        List<string> cmdArgs = new();

        var compiledPch = GetCompiledPchLocation(project);
        if (!string.IsNullOrWhiteSpace(project.PchHeader) && !string.IsNullOrWhiteSpace(compiledPch))
        {
            cmdArgs.Add("-include");
            //remove the .gch extension
            cmdArgs.Add(compiledPch[..^PchExt.Length]);
        }
        
        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);

        if (GenerateSourceDependencies)
            cmdArgs.Add("-MMD");

        if (Opt.GetTarget().CompileInfo.TryGetValue(project.Language, out var info))
        {
            cmdArgs.AddRange(info.Arguments);
        }

        AddDefines(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);
        
        cmdArgs.Add("-o");
        cmdArgs.Add(outputFile);

        cmdArgs.Add("-c");
        cmdArgs.Add(sourceFile);
        
        var useCpp = project.Language == Lang.Cpp;

        var compiler = sourceFile.EndsWith(".cpp") ? CppCompilerElf : CCompilerElf;
        
        CompileDatabase?.Add(new CompileCommands.CompileCommand
        {
            Directory = project.Directory,
            Arguments = cmdArgs.ToArray(),
            Command = compiler + " " + Strings.Join(cmdArgs.ToArray(), " "),
            File = sourceFile,
            Output = outputFile
        });
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), compiler,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }

    public override ProcUtil.RunOutput LinkProject(Project inPrj, string[] objects)
    {
        if(inPrj is not CProject project)
            throw new Exception("Project is not a CppProject or CProject");
        
        var outputPath = project.GetOutputFilePath(Opt);
        
        if(project.Type == BinType.StaticLib)
            return ProcUtil.RunCmdOptLog(Opt.GetTarget(), "ar", $"-rcs \"{outputPath}\" " + string.Join(" ", objects.ToArray()),
                project.Directory, Opt.JustPrint);
        
        List<string> cmdArgs = new();

        AddLinkOptions_Early(project, ref cmdArgs);
        
        AddOutput(project, ref cmdArgs);

        AddStdVersion(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);

        cmdArgs.AddRange(objects);

        foreach (var rpath in project.GetRPaths(project.GetOutputDirectory(Opt), Opt))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                cmdArgs.Add($"-Wl,-rpath,@loader_path/{rpath}");
            else
                cmdArgs.Add($"-Wl,-rpath,$ORIGIN/{rpath}");
        }

        
        if (Opt.GetTarget().CompileInfo.TryGetValue(project.Language, out var info))
        {
            cmdArgs.AddRange(info.LinkArguments);
        }
        
        AddLibraryPaths(project, ref cmdArgs);
        AddLibraries(project, ref cmdArgs);
        AddStaticStd(project, ref cmdArgs);
        AddSymbols(project, ref cmdArgs);

        switch (project.Type)
        {
            case BinType.SharedObj:
                cmdArgs.Add("-shared");
                break;
            case BinType.StaticLib:
                cmdArgs.Add("-static");
                break;
        }
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), project.Language == Lang.C ? CCompilerElf : CppCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }

    public override string GetCompiledPchLocation(CProject project)
    {
        var fileName = Path.GetFileName(project.PchHeader);
        return Path.Combine(project.GetIntermediateDirectory(Opt), fileName + PchExt);
    }

    public override (bool success, string[] dependencies) GetDependencies(Project project, string objectFile)
    {
        //find dependency file for source file
        //this will be the source files name with .d appended located in the projects intermediate directory
        var depFile = Path.Combine(project.GetIntermediateDirectory(Opt),
            Path.GetFileNameWithoutExtension(objectFile) + ".d");
        
        var dependencies = Array.Empty<string>();

        if (!File.Exists(depFile))
            return (false, dependencies);

        var dep = PosixDepParser.Parse(File.ReadAllText(depFile));
        var objFileAbs = Path.Combine(project.GetIntermediateDirectory(Opt), objectFile);
        if (!dep.ContainsKey(objFileAbs))
            return (false, dependencies);
        dependencies = dep[objFileAbs].ToArray();
        return (true, dependencies);
    }

    public override ProcUtil.RunOutput CompilePch(CProject project)
    {
        var pchHeader = project.GetPathAbs(project.PchHeader);
        var pchObj = GetCompiledPchLocation(project);
        
        //due to how gcc works we need to make a dummy file with the pch headers name in the intermediate directory
        var dummyFile = Path.Combine(project.IntermediateDirectory, Path.GetFileName(pchHeader));
        File.WriteAllText(dummyFile, "");
        
        var cmdArgs = new List<string>();
        cmdArgs.Add("-x");
        cmdArgs.Add(project is CppProject ? "c++-header" : "c-header");
        cmdArgs.Add("-MMD");
        
        if (Opt.GetTarget().CompileInfo.TryGetValue(project.Language, out var info))
        {
            cmdArgs.AddRange(info.Arguments);
        }
        
        AddDefines(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);
        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);
        cmdArgs.Add("-o");
        cmdArgs.Add(pchObj);
        cmdArgs.Add("-c");
        cmdArgs.Add(pchHeader);
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), project.Language == Lang.C ? CCompilerElf : CppCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }

    public virtual void AddLinkOptions_Early(CProject project, ref List<string> args)
    {
        
    }
    
    public virtual void AddOutput(CProject project, ref List<string> args)
    {
        var outputPath = project.GetOutputFilePath(Opt);
        args.Add("-o");
        args.Add(outputPath);
    }
    
    public virtual void AddSymbols(CProject project, ref List<string> args)
    {
        args.Add(project.Symbols ? "-g" : "-s");
    }
    
    public virtual void AddStdVersion(CProject project, ref List<string> args)
    {
        if (project.StdVersion == "none")
        {
            args.Add("-nostdlib");
            return;
        }

        if (project.StdVersion != string.Empty)
        {
            if (project.StdVersion.All(char.IsDigit))
            {
                args.Add($"-std=" + (project is CppProject ? "c++" : "c") + project.StdVersion);
            }
            else
            {
                //just pass what ever they put in
                args.Add($"-std=" + project.StdVersion);
            }
        }
    }
    
    
    public void AddStaticStd(CProject project, ref List<string> args)
    {
        if (project.StaticStdLib)
        {
            args.Add("-static-libgcc");
            args.Add("-static-libstdc++");
            args.Add("-Wl,-Bstatic");
            args.Add("-lstdc++");
            args.Add("-lpthread");
            args.Add("-Wl,-Bdynamic");
        }
    }

    public void AddPic(CProject project, ref List<string> args)
    {
        if (project.UsePIC) args.Add("-fPIC");
    }

    public void AddDefines(CProject project, ref List<string> args)
    {
        var target = Opt.GetTarget();
        if (target.CompileInfo.TryGetValue(project.Language, out var value))
        {
            foreach (var define in value.Defines)
            {
                args.Add(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");
            }
        }

        foreach (var define in project.GetDefines())
            args.Add(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");
    }

    public void AddIncludes(CProject project, ref List<string> args)
    {
        foreach (var include in project.GetIncludePaths()) args.Add($"-I{include}");
    }

    public void AddLibraryPaths(CProject project, ref List<string> args)
    {
        foreach (var libraryPath in project.GetLibraryPaths(Opt)) args.Add($"-L{libraryPath}");
    }

    public virtual void AddLibraries(CProject project, ref List<string> args)
    {
        foreach (var library in project.GetLibraries(Opt)) args.Add($"-l{library}");
    }

    public void AddOptimisation(CProject project, ref List<string> args)
    {
        if (project.Optimisation != String.Empty)
        {
            args.Add($"-O{project.Optimisation}");
        }
    }
}