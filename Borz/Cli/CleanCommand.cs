using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class CleanCommand : Command<CleanCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Core.Borz.CleanWorkspace(Directory.GetCurrentDirectory());
        return 0;
    }
}