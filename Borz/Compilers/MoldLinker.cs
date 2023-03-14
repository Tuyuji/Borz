using AkoSharp;

namespace Borz.Compilers;

[ShortType("Mold")]
public class MoldLinker : GccCompiler
{
    public MoldLinker()
    {
        UseMold= true;
    }
}