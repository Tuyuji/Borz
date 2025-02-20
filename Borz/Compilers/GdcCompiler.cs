using Borz.Helpers;
using Borz.Languages.C;
using Borz.Languages.D;
using Borz.Lua;
using Microsoft.VisualBasic;

namespace Borz.Compilers;

public class GdcCompiler : CommonUnixCCompiler
{
    public GdcCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("gdc", "gdc");
    }

    public override string Name => "GDC";
    public override (bool supported, string reason) IsSupported()
    {
        return (true, "");
    }

    public override ProcUtil.RunOutput CompileObject(Project inPrj, string sourceFile, string outputFile)
    {
        if (inPrj is not DProject project)
            throw new ArgumentException("Project must be a DProject");
        
        List<string> cmdArgs = new();
        
        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);
        
        if(GenerateSourceDependencies)
            cmdArgs.Add("-MMD");
        
        AddVersion(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);
        
        if (project.Type == BinType.SharedObj || project.Type == BinType.StaticLib)
        {
            var absIncludeDir = project.GetGeneratedIncludeDirectory(Opt);
            if(!Directory.Exists(absIncludeDir))
                Directory.CreateDirectory(absIncludeDir);
            
            var includeDir = Path.GetRelativePath(project.Directory, project.GetGeneratedIncludeDirectory(Opt));
            cmdArgs.Add("-op");
            cmdArgs.Add("-Hd");
            cmdArgs.Add(includeDir);
        }
        
        cmdArgs.Add("-o");
        cmdArgs.Add(outputFile);
        
        cmdArgs.Add("-c");
        cmdArgs.Add(sourceFile);

        CompileDatabase?.Add(new CompileCommands.CompileCommand
        {
            Directory = project.Directory,
            Arguments = cmdArgs.ToArray(),
            Command = CCompilerElf + " " + Strings.Join(cmdArgs.ToArray(), " "),
            File = sourceFile,
            Output = outputFile
        });
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), CCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }

    public override ProcUtil.RunOutput LinkProject(Project inPrj, string[] objects)
    {
        if(inPrj is not DProject project)
            throw new ArgumentException("Project must be a DProject");
        
        var outputPath = project.GetOutputFilePath(Opt);
        
        if(project.Type == BinType.StaticLib)
            return ProcUtil.RunCmdOptLog(Opt.GetTarget(), "ar", $"-rcs \"{outputPath}\" " + string.Join(" ", objects.ToArray()),
                project.Directory, Opt.JustPrint);
        
        List<string> cmdArgs = new();

        AddLinkOptions_Early(project, ref cmdArgs);
        
        AddOutput(project, ref cmdArgs);

        AddVersion(project, ref cmdArgs);
        AddPhobos(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);

        cmdArgs.AddRange(objects);

        foreach (var rpath in project.GetRPaths(project.GetOutputDirectory(Opt), Opt))
            cmdArgs.Add($"-Wl,-rpath=$ORIGIN/{rpath}");
        
        if (Opt.GetTarget().CompileInfo.TryGetValue(project.Language, out var info))
        {
            cmdArgs.AddRange(info.LinkArguments);
        }
        
        AddLibraryPaths(project, ref cmdArgs);
        AddLibraries(project, ref cmdArgs);
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
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), CCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }

    private void AddVersion(DProject project, ref List<string> cmdArgs)
    {
        foreach (var version in project.Versions) cmdArgs.Add("-fversion=" + version);
    }

    public override void AddSymbols(CProject inPrj, ref List<string> args)
    {
        if (inPrj is not DProject project)
            throw new ArgumentException("Project must be a DProject");
        
        args.Add(project.Symbols ? "-fdebug" : "-frelease");
        args.Add(project.Symbols ? "-g" : "-s");
    }
    
    public override void AddStdVersion(CProject inPrj, ref List<string> args)
    {
        if (inPrj is not DProject project)
            throw new ArgumentException("Project must be a DProject");
        
        if (project.StdVersion == "none")
        {
            args.Add("-nostdlib");
            return;
        }

        if (project.StdVersion != string.Empty)
        {
            args.Add($"-std=" + project.StdVersion);
        }
    }

    public void AddPhobos(DProject project, ref List<string> args)
    {
        if(project.PhobosType == PhobosType.NotSet)
            return;
        
        switch (project.PhobosType)
        {
            case PhobosType.None:
                args.Add("-nophoboslib");
                return;
            case PhobosType.Static:
                args.Add("-static-libphobos");
                return;
            case PhobosType.Shared:
                args.Add("-shared-libphobos");
                return;
            default:
                return;
        }
    }
}