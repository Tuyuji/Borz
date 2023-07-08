using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class CompileCommand : Command<CompileCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [LocalDesc("Compile.Desc.JustLog")]
        [CommandOption("-n|--just-log")]
        [DefaultValue(false)]
        public bool JustLog { get; init; }

        [CommandArgument(0, "[config]")] public string? Config { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Core.Borz.BuildConfig.Config = settings.Config ?? "debug";
        Core.Borz.RunScript(Directory.GetCurrentDirectory());

        return
            Core.Borz.CompileWorkspace(settings.JustLog)
                ? 0
                : 1;
    }
}