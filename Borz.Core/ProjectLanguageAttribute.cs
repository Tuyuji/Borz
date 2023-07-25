namespace Borz.Core;

public class ProjectLanguageAttribute : Attribute
{
    public string Language { get; }

    public ProjectLanguageAttribute(string language)
    {
        Language = language;
    }
}