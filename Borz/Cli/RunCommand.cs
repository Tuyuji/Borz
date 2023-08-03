using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Borz.Core;
using Borz.Resources;
using Spectre.Console.Cli;

namespace Borz.Cli;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class RunCommand : Command<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")] public string Name { get; set; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Workspace.Init(Directory.GetCurrentDirectory());

        //see if the name is in Workspace, the projects is a list not a dictionary.
        if (Workspace.Projects.All(x => x.Name != settings.Name))
        {
            Console.WriteLine(Lang.Run_Error_ProjectNotFound);
            return 1;
        }

        var proj = Workspace.Projects.First(x => x.Name == settings.Name);
        if (!(proj.Type is BinType.ConsoleApp or BinType.WindowsApp))
        {
            Console.WriteLine(Lang.Init_Error_ProjectNotExe);
            return 1;
        }

        //Well this is a bit hacky, but it works.
        var exe = Path.Combine(proj.OutputDirectory, proj.Name);
        if (OperatingSystem.IsWindows()) exe += ".exe";

        if (!File.Exists(exe))
        {
            Console.WriteLine(Lang.Run_Error_ExeNotFound);
            return 1;
        }

        Process.Start(exe).WaitForExit();

        return 0;
    }
}