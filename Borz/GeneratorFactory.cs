using Borz.Generators;
using Borz.Languages.C;

namespace Borz;

public class GeneratorFactory
{
    private static Dictionary<string, Generator> _knownGenerators = new();

    static GeneratorFactory()
    {
        _knownGenerators.Add("jb", new JetbrainsGenerator());
        _knownGenerators.Add("ninja", new NinjaGenerator());
        _knownGenerators.Add("nml", new NomnomlGen());
    }

    public static Generator? TryGetGenerator(string name)
    {
        return _knownGenerators.GetValueOrDefault(name);
    }

    public static Generator GetGenerator(string name)
    {
        return TryGetGenerator(name) ?? throw new Exception($"Unknown generator: {name}");
    }
}