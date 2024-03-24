using Borz.Languages.C;

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
        var result = ProcUtil.RunCmd(CCompilerElf, "--version");
        if (result.Exitcode != 0)
        {
            return (false, $"Failed to run gcc --version: {result.Ouput}");
        }

        return (true, string.Empty);
    }
}