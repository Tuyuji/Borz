namespace Borz.Compilers;

public class GccCompiler : CommonUnixCCompiler
{
    public override string Name => "gcc";


    public GccCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("gcc", "gcc");
        CppCompilerElf = Opt.GetTarget().GetBinaryPath("g++", "g++");
    }

    public override (bool supported, string reason) IsSupported()
    {
        var result = ProcUtil.RunCmd(CCompilerElf, "-dumpfullversion");
        if (result.Exitcode != 0)
        {
            return (false, $"Failed to run gcc -dumpfullversion: {result.Ouput}");
        }

        int major, minor, patch;
        var version = result.Ouput.Split('.');
        major = int.Parse(version[0]);
        minor = int.Parse(version[1]);
        patch = int.Parse(version[2]);

        if (major < 12)
        {
            return (false, $"Need major version 13+, current is: {major}.{minor}.{patch}");
        }
        
        return (true, string.Empty);
    }
}