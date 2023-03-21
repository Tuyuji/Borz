using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        [Description("Watch for changes and regenerate.")]
        [CommandOption("-w|--watch")]
        [DefaultValue(false)]
        public bool Watch { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        //This runs the build.borz command in the current directory.
        Core.Borz.GenerateWorkspace(Directory.GetCurrentDirectory());
        return 0;
    }
}