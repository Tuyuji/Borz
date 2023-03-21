namespace Borz.Core;

public class ProjectLanguageAttribute : Attribute
{
    public Language Language { get; }

    public ProjectLanguageAttribute(Language language)
    {
        Language = language;
    }
}