using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Borz.Core;
using Borz.Core.Lua;
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

        [LocalDesc("Compile.Desc.Platform")]
        [CommandOption("-p|--platform")]
        [DefaultValue(typeof(Platform), Core.Lua.Platform.Unknown)]
        public string Platform { get; init; }

        [CommandArgument(0, "[config]")] public string? Config { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Workspace.BuildCfg.Config = settings.Config ?? "debug";
        Workspace.BuildCfg.TargetPlatform = settings.Platform;
        Workspace.Init(Directory.GetCurrentDirectory());

        Core.Borz.Script.CallPreCompileEvent();
        if (Workspace.Compile(settings.JustLog))
        {
            Core.Borz.Script.CallPostCompileEvent();
            return 0;
        }

        return 1;
    }
}