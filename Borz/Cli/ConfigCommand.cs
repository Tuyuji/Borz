using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Borz.Cli;

public class ConfigSettings : CommandSettings
{
}

public class ListConfigSettings : ConfigSettings
{
}

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class ListConfigCommand : Command<ListConfigSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] ListConfigSettings settings)
    {
        //TODO: List config
        AnsiConsole.WriteLine("TODO: List config");
        return 0;
    }
}