using Borz.Builders;

namespace Borz;

public interface IBuilder
{
    bool Build(Project project, bool justLog = false);
    
    static IBuilder GetBuilder(Project project)
    {
        switch (project.Language)
        {
            case Language.C:
            case Language.Cpp:
                return new CppBuilder();
            default:
                throw new Exception("Language not supported");
        }
    }
}