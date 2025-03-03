namespace Borz.Compilers;

public class UnixCCompiler : CommonUnixCCompiler
{
    public override string Name => "unix";
    
    public UnixCCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("cc", "cc");
        CppCompilerElf = Opt.GetTarget().GetBinaryPath("c++", "c++");
    }

    public override (bool supported, string reason) IsSupported()
    {
        //cant test this.
        return (true, string.Empty);
    }
}