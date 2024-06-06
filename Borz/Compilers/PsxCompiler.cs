using Borz.Helpers;
using Borz.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Compilers;

public class PsxCompiler : CommonUnixCCompiler
{
    private string _psxDir = "/home/drogonmar/.config/borz/third_party/";

    private string[] _extraArgs = new[]
    {
        "-ffunction-sections",
        "-fdata-sections",
        "-mno-gpopt",
        "-fomit-frame-pointer",
        "-fno-builtin",
        "-fno-strict-aliasing",
        "-Wno-attributes",
    };
    
    private string[] _commonArgs = new[]
    {
        "-march=mips1",
        "-mabi=32",
        "-EL",
        "-fno-pic",
        "-mno-shared",
        "-mno-abicalls",
        "-mfp32",
        "-mno-llsc",
        "-fno-stack-protector",
        "-nostdlib",
        "-ffreestanding"
    };
    
    public PsxCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("gcc", "gcc");
        CppCompilerElf = Opt.GetTarget().GetBinaryPath("g++", "g++");
    }

    public override string Name => "PsxCompiler";
    public override (bool supported, string reason) IsSupported()
    {
        if (Opt.GetTarget().OS != "psx")
        {
            return (false, "Target's OS isn't psx");
        }

        if (Opt.GetTarget().Arch != "mipsel")
        {
            return (false, "Target's arch isn't mipsel");
        }
        
        return (true, String.Empty);
    }

    public void CheckProject(CProject project)
    {
        if (project.UsePIC == true)
            throw new Exception("PIC isnt supported for PSX");
    }

    public override ProcUtil.RunOutput CompileObject(Project inPrj, string sourceFile, string outputFile)
    {
        if(inPrj is not CProject project)
            throw new Exception("Project is not a CppProject or CProject");

        CheckProject(project);
        
        List<string> cmdArgs = new();
        cmdArgs.Add("-I" + Path.Combine(_psxDir, "psyq-iwyu", "include"));
        cmdArgs.AddRange(_extraArgs);
        cmdArgs.AddRange(_commonArgs);
        cmdArgs.Add("-I" + Path.Combine(_psxDir, "nugget"));
        AddSymbols(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);
        
        cmdArgs.Add("-o");
        cmdArgs.Add(outputFile);

        cmdArgs.Add("-c");
        cmdArgs.Add(sourceFile);
        
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
        if (inPrj is not CProject project)
            throw new Exception("Project is not a CppProject or CProject");
                
        CheckProject(project);
        
        var outputPath = project.GetOutputFilePath(Opt);
        
        if(project.Type == BinType.StaticLib)
            return ProcUtil.RunCmdOptLog(Opt.GetTarget(), "ar", $"-rcs \"{outputPath}\" " + string.Join(" ", objects.ToArray()),
                project.Directory, Opt.JustPrint);
        
        List<string> cmdArgs = new();
        
        cmdArgs.Add("-o");
        cmdArgs.Add(outputPath);
        
        cmdArgs.AddRange(objects);

        AddOptimisation(project, ref cmdArgs);
        
        if (GenerateSourceDependencies)
            cmdArgs.Add("-MMD");
        
        cmdArgs.Add("-L" + Path.Combine(_psxDir, "psyq", "lib"));
        AddLibraryPaths(project, ref cmdArgs);
        
        cmdArgs.Add("-Wl,--start-group");
        AddLibraries(project, ref cmdArgs);
        cmdArgs.Add("-Wl,--end-group");
        cmdArgs.Add($"-Wl,-Map={project.Name}.map");
        
        cmdArgs.Add("-nostdlib");
        
        cmdArgs.AddRange(new []
        {
            "-T" + Path.Combine(_psxDir, "nugget", "nooverlay.ld"),
            "-T" + Path.Combine(_psxDir, "nugget", "ps-exe.ld")
        });
        
        cmdArgs.Add("-static");
        cmdArgs.Add("-Wl,--gc-sections");
        cmdArgs.AddRange(_commonArgs);
        cmdArgs.Add("-Wl,--oformat=elf32-littlemips");
        AddSymbols(project, ref cmdArgs);
        AddOptimisation(project, ref cmdArgs);
        
        return ProcUtil.RunCmdOptLog(Opt.GetTarget(), project.Language == Lang.C ? CCompilerElf : CppCompilerElf,
            Strings.Join(cmdArgs.ToArray())!, project.Directory, Opt.JustPrint);
    }
}