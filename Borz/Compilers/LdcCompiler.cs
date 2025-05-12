using Borz.Languages.C;
using Borz.Languages.D;

namespace Borz.Compilers;

public class LdcCompiler : DCompiler
{
    public LdcCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("ldc2", "ldc2");
    }
    
    public override string Name => "LDC";

    public override (bool supported, string reason) IsSupported()
    {
        return (true, string.Empty);
    }

    protected override void AddMakeDeps(DProject project, string outputPath, ref List<string> args)
    {
        if(GenerateSourceDependencies)
            args.Add($"--makedeps={outputPath}");
    }

    protected override string GetDependencyFileExt()
    {
        return ".dep";
    }
    
    public override void AddSymbols(CProject inPrj, ref List<string> args)
    {
        if (inPrj is not DProject project)
            throw new ArgumentException("Project must be a DProject");
        
        args.Add(project.Symbols ? "--d-debug" : "--release");
        args.Add(project.Symbols ? "-g" : "-s");
    }

    public override void AddOutput(CProject project, ref List<string> args)
    {
        var outputPath = project.GetOutputFilePath(Opt);
        args.Add($"-of={outputPath}");
    }

    protected override void AddObjectOutput(DProject project, string outputFile, ref List<string> args)
    {
        args.Add($"-of={outputFile}");
    }
}