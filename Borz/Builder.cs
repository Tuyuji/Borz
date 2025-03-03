namespace Borz;

public abstract class Builder
{
    public abstract (bool success, string error) Build(Project project, Options opt);
}