namespace Borz;

public abstract class Generator
{
    public abstract (bool success, string error) Generate(Workspace ws, Options opt);
} 