namespace Borz.Core;

public interface IBuilder
{
    bool Build(Project project, bool justLog = false);
}