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
        [Description("Print the commands that would be executed.")]
        [CommandOption("-n|--just-log")]
        [DefaultValue(false)]
        public bool JustLog { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return
            Core.Borz.CompileWorkspace(Directory.GetCurrentDirectory(), settings.JustLog)
                ? 0
                : 1;
    }
}