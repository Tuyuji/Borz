using Borz.Languages.C;

namespace Borz;

public class BuildFactory
{
    private static Dictionary<string, Builder> _knownBuilders = new();

    static BuildFactory()
    {
        var cBuilder = new CBuilder();
        _knownBuilders.Add(Lang.C, cBuilder);
        _knownBuilders.Add(Lang.Cpp, cBuilder);
    }

    public static Builder GetBuilder(string language)
    {
        if (_knownBuilders.TryGetValue(language, out var builder)) return builder;

        throw new Exception($"No builder found for language '{language}'");
    }
}