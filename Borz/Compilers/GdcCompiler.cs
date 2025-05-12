namespace Borz.Compilers;

public class GdcCompiler : DCompiler
{
    public GdcCompiler(Options opt) : base(opt)
    {
        CCompilerElf = Opt.GetTarget().GetBinaryPath("gdc", "gdc");
    }

    public override string Name => "GDC";

    public override (bool supported, string reason) IsSupported()
    {
        return (true, string.Empty);
    }
}