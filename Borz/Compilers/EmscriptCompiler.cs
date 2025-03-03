using Borz.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Compilers;

public class EmscriptCompiler : CommonUnixCCompiler
{
    public override string Name => "emscript";

    public EmscriptCompiler(Options opt) : base(opt)
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