namespace Borz;

public abstract class Compiler
{
    public abstract string Name { get; }

    public Options Opt;
    
    public abstract (bool supported, string reason) IsSupported();

    public Compiler(Options opt)
    {
        Opt = opt;
    }
}