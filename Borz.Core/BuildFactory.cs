using Borz.Core.Languages.C;

namespace Borz.Core;

public class BuildFactory
{
    private static Dictionary<string, IBuilder> _knownBuilders = new();

    static BuildFactory()
    {
        var cBuilder = new CppBuilder();
        _knownBuilders.Add(Language.C, cBuilder);
        _knownBuilders.Add(Language.Cpp, cBuilder);
    }

    public static IBuilder GetBuilder(string language)
    {
        if (_knownBuilders.TryGetValue(language, out var builder))
        {
            return builder;
        }

        throw new Exception($"No builder found for language '{language}'");
    }
}