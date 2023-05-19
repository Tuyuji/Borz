using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Borz.Core;
using Spectre.Console;
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
        Core.Borz.RunScript(Directory.GetCurrentDirectory());

        //see if the name is in Workspace, the projects is a list not a dictionary.
        if (Workspace.Projects.All(x => x.Name != settings.Name))
        {
            AnsiConsole.WriteLine("Project not found.");
            return 1;
        }

        var proj = Workspace.Projects.First(x => x.Name == settings.Name);
        if (!(proj.Type is BinType.ConsoleApp or BinType.WindowsApp))
        {
            AnsiConsole.WriteLine("Project is not a console app or a windows app.");
            return 1;
        }

        //Well this is a bit hacky, but it works.
        string exe = Path.Combine(proj.OutputDirectory, proj.Name);
        if (OperatingSystem.IsWindows())
        {
            exe += ".exe";
        }

        if (!File.Exists(exe))
        {
            AnsiConsole.WriteLine("Executable not found.");
            return 1;
        }

        Process.Start(exe).WaitForExit();

        return 0;
    }
}