using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Borz.Core;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class CleanCommand : Command<CleanCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [LocalDesc("Compile.Desc.JustLog")]
        [CommandOption("-n|--just-log")]
        [DefaultValue(false)]
        public bool JustLog { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Workspace.Init(Directory.GetCurrentDirectory());
        Core.Borz.CleanWorkspace(settings.JustLog);
        return 0;
    }
}