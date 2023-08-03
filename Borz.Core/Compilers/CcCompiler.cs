using System.Collections.Concurrent;
using Borz.Core.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Core.Compilers;

public abstract class CcCompiler : ICCompiler
{
    public bool JustLog { get; set; }

    public CcCompiler()
    {
    }

    public bool GenerateSourceDependencies { get; set; }
    public bool GenerateCompileCommands { get; set; }

    public ConcurrentBag<CppBuilder.CompileCommand> CompileCommands { get; } = new();
    public bool OnlyOutputCompileCommands { get; set; } = false;

    public abstract string CCompilerElf { get; }
    public abstract string CppCompilerElf { get; }

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

        var compiledPch = GetCompiledPchLocation(project);
        if (!string.IsNullOrWhiteSpace(project.PchHeader) && !string.IsNullOrWhiteSpace(compiledPch))
        {
            cmdArgs.Add("-include");
            //remove the .gch extension
            cmdArgs.Add(compiledPch[..^4]);
        }

        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);

        if (GenerateSourceDependencies)
            cmdArgs.Add("-MMD");

        AddDefines(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);
        AddLibraries(project, ref cmdArgs);

        cmdArgs.Add("-o");
        cmdArgs.Add(outputFile);

        cmdArgs.Add("-c");
        cmdArgs.Add(sourceFile);

        var useCpp = project.Language == Language.Cpp;

        var compiler = sourceFile.EndsWith(".cpp") ? CppCompilerElf : CCompilerElf;

        if (GenerateCompileCommands)
            CompileCommands.Add(
                new CppBuilder.CompileCommand()
                {
                    Directory = project.ProjectDirectory,
                    Arguments = cmdArgs.ToArray(),
                    Command = compiler + " " + Strings.Join(cmdArgs.ToArray(), " "),
                    File = sourceFile,
                    Output = outputFile
                });

        var res = Utils.RunCmd(compiler,
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
        return res;
    }


    public UnixUtil.RunOutput LinkProject(Project unknownProject, string[] objects)
    {
        CProject project = null;
        if (unknownProject is not (CProject or CppProject))
            throw new Exception("Project is not a CppProject or CProject");

        project = unknownProject as CProject ?? throw new InvalidOperationException();

        var outputPath = project.OutputDirectory;
        var outputName = project.GetOutputName();

        if (project.Type == BinType.StaticLib)
        {
            var output = Path.Combine(outputPath, $"lib{outputName}.a");
            return Utils.RunCmd("ar", $"-rcs \"{output}\" " + string.Join(" ", objects.ToArray()),
                project.ProjectDirectory, JustLog);
        }

        List<string> cmdArgs = new();

        outputPath = project.GetOutputFilePath();

        cmdArgs.Add("-o");
        cmdArgs.Add(outputPath);

        AddStdVersion(project, ref cmdArgs);

        cmdArgs.AddRange(objects);

        foreach (var rpath in project.GetRPaths(unknownProject.GetPathAbs(unknownProject.OutputDirectory)))
            cmdArgs.Add($"-Wl,-rpath=$ORIGIN/{rpath}");

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


        if (Borz.UseMold) cmdArgs.Add("-fuse-ld=mold");

        var res = Utils.RunCmd(project.Language == Language.C ? CCompilerElf : CppCompilerElf,
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

    public static bool supported = false;

    public abstract bool IsSupportedExt(out string reason);

    public bool IsSupported(out string reason)
    {
        if (supported)
        {
            reason = "";
            return true;
        }

        return IsSupportedExt(out reason);
    }

    public string GetFriendlyName(bool asLinker)
    {
        if (Borz.UseMold && asLinker) return GetFriendlyName() + " w/ Mold";

        return GetFriendlyName();
    }

    public string GetCompiledPchLocation(CProject project)
    {
        //should be ProjectIntDir/pch.h.gch
        var fileName = Path.GetFileName(project.PchHeader);
        return Path.Combine(project.IntermediateDirectory, fileName + ".gch");
    }

    public UnixUtil.RunOutput CompilePch(CProject project)
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
        //add defines
        AddDefines(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);
        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);
        cmdArgs.Add("-o");
        cmdArgs.Add(pchObj);
        cmdArgs.Add("-c");
        cmdArgs.Add(pchHeader);
        var res = Utils.RunCmd(project.Language == Language.C ? CCompilerElf : CppCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
        return res;
    }

    public void SetJustLog(bool justLog)
    {
        JustLog = justLog;
    }

    public abstract string GetFriendlyName();

    public void AddSymbols(CProject project, ref List<string> args)
    {
        if (project.Symbols)
            args.Add("-g");
        else
            args.Add("-s");
    }

    public void AddStdVersion(CProject project, ref List<string> args)
    {
        if (project.StdVersion == "none")
        {
            args.Add("-nostdlib");
            return;
        }

        if (project.StdVersion != string.Empty)
        {
            if (project.StdVersion.All(char.IsDigit))
                args.Add($"-std=" + (project is CppProject ? "c++" : "c") + project.StdVersion);
            else
                //just pass what ever they put in
                args.Add($"-std=" + project.StdVersion);
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
        var platformInfo = Borz.BuildConfig.TargetInfo;
        if (platformInfo.Defines != null)
            foreach (var define in platformInfo.Defines)
                args.Add(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");

        foreach (var define in project.GetDefines())
            args.Add(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");
    }

    public void AddIncludes(CProject project, ref List<string> args)
    {
        foreach (var include in project.GetIncludePaths()) args.Add($"-I{include}");
    }

    public void AddLibraryPaths(CProject project, ref List<string> args)
    {
        foreach (var libraryPath in project.GetLibraryPaths()) args.Add($"-L{libraryPath}");
    }

    public void AddLibraries(CProject project, ref List<string> args)
    {
        foreach (var library in project.GetLibraries()) args.Add($"-l{library}");
    }
}