using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Borz.Core;
using Borz.Resources;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class GenerateCommand : Command<GenerateCommand.Settings>
{
    private static FileSystemWatcher _watcher;
    private static bool _watching;

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<generator>")] public string Generator { get; set; }

        [LocalDesc("Generate.Desc.Watch")]
        [CommandOption("-w|--watch")]
        [DefaultValue(false)]
        public bool Watch { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        //This runs the build.borz command in the current directory.
        Workspace.Init(Directory.GetCurrentDirectory());
        if (!Core.Borz.GenerateWorkspace(settings.Generator.ToLower()))
        {
            Console.WriteLine(Lang.Generate_Error_UnknownGenerator, settings.Generator);
            Console.WriteLine(Lang.Generate_ListAvailableHeader);
            foreach (var generator in Core.Borz.Generators)
            {
                Console.WriteLine($@"  {generator.Key}");
            }
        }

        return 0;
    }
}