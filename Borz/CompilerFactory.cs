using Borz.Compilers;

namespace Borz;

public class CompilerFactory
{
    private delegate Compiler CompilerCreationDelete(Options opt);
    
    private static Dictionary<string, CompilerCreationDelete> _knownCompilers;

    static CompilerFactory()
    {
        _knownCompilers = new Dictionary<string, CompilerCreationDelete>()
        {
            { "unix", opt => new UnixCCompiler(opt) },
            { "gcc", opt => new GccCompiler(opt) },
            { "gdc", opt => new GdcCompiler(opt) },
            { "psxgcc", opt => new PsxCompiler(opt) },
            { "emcc", opt => new EmscriptCompiler(opt) },
        };
    }

    public static string[] GetKnownCompilerNames()
    {
        return _knownCompilers.Keys.ToArray();
    }
    
    public static T GetCompiler<T>(string language, Options opt) where T: Compiler
    {
        var targetCompiler = string.Empty;
        if (opt.Target == null)
        {
            //no cross compiling, just return whats in the config
            targetCompiler = Borz.Config.Get("compilers", language);
        }
        else
        {
            if (!opt.Target.Compilers.TryGetValue(language, out string? value))
            {
                throw new Exception($"No compiler defined for language \"{language}\" for {opt.Target}");
            }
            targetCompiler = value;
        }
        return (T)_knownCompilers[targetCompiler](opt);
    }
}