using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Borz.Compilers;
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
        //This runs the build.borz command in the current directory.
        Workspace.Init();

        Utils.UpdateMemInfo();
        Workspace.Projects.ForEach(prj =>
        {
            MugiLog.Info("===========================================");
            var builder = IBuilder.GetBuilder(prj);
            builder.Build(prj, settings.JustLog);
        });
        return 0;
    }
}