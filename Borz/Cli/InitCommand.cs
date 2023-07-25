using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Borz.Core;
using Borz.Resources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")] public string Name { get; set; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        if (Directory.Exists(settings.Name))
        {
            Console.WriteLine(Lang.Init_Error_DirectoryExists);
            return 1;
        }

        //see if the name is a valid directory name.
        if (settings.Name.IndexOfAny(Path.GetInvalidPathChars()) != -1)
        {
            Console.WriteLine(Lang.Init_Error_InvalidDirectoryName);
            return 1;
        }

        var language = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(Lang.Init_Choice_Language)
                .AddChoices(Language.C, Language.Cpp)
        );

        Directory.CreateDirectory(settings.Name);

        Directory.SetCurrentDirectory(settings.Name);

        try
        {
            switch (language)
            {
                case Language.C:
                case Language.Cpp:
                    GenerateCFolder(settings.Name, language == Language.Cpp);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(Lang.Init_Error_Throw + ex.Message);
            Directory.SetCurrentDirectory("..");
            Directory.Delete(settings.Name);
            return 1;
        }

        return 0;
    }

    private void GenerateCFolder(string name, bool isCpp)
    {
        var binType = AnsiConsole.Prompt(
            new SelectionPrompt<BinType>()
                .Title(Lang.Init_Choice_BinaryType)
                .AddChoices(BinType.ConsoleApp, BinType.SharedObj, BinType.StaticLib)
        );

        Directory.CreateDirectory("Source");
        Directory.CreateDirectory("Include");

        var buildRaw = GetResource("Borz.Resources.Init.buildc.borz");
        var build = buildRaw.Replace("$NAME", name)
            .Replace("$LANG", isCpp ? "Cpp" : "C")
            .Replace("$BINTYPE", binType.ToString())
            .Replace("$SRCEXT", isCpp ? "cpp" : "c");

        File.WriteAllText("build.borz", build);
        File.WriteAllText("borzsettings.ako", GetResource("Borz.Resources.Init.borzsettings.ako"));

        var resourceLocationSrc = "Borz.Resources.Init." + (isCpp ? "Cpp" : "C");

        switch (binType)
        {
            case BinType.ConsoleApp:
                File.WriteAllText(
                    "Source/main." + (isCpp ? "cpp" : "c"),
                    GetResource(resourceLocationSrc + ".ConsoleMain." + (isCpp ? "cpp" : "c")));
                break;
        }
    }

    private string GetResource(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(path);
        if (stream == null)
            throw new Exception("Resource not found: " + path);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}