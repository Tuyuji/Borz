using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Borz.Core;
using Borz.Core.Generators;
using Borz.Resources;
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
        [CommandArgument(0, "<generator>")] public string Generator { get; set; }

        [LocalDesc("Generate.Desc.Watch")]
        [CommandOption("-w|--watch")]
        [DefaultValue(false)]
        public bool Watch { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        //Find all classes that implement IGenerator with the attribute FriendlyNameAttribute.
        var genTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IGenerator).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract &&
                        p.GetCustomAttribute<FriendlyNameAttribute>() != null)
            .ToArray();
        //now get the friendly names of all of them.
        var genNames = genTypes.Select(t => t.GetCustomAttribute<FriendlyNameAttribute>()?.FriendlyName.ToLower())
            .ToArray();

        //find the index of the generator name in the list of friendly names.
        //use tolower
        var genIndex = Array.IndexOf(genNames, settings.Generator.ToLower());
        if (genIndex == -1)
        {
            Console.WriteLine(Lang.Generate_Error_UnknownGenerator, settings.Generator);
            Console.WriteLine(Lang.Generate_ListAvailableHeader);
            foreach (var genName in genNames) Console.WriteLine($@"  {genName}");

            return 1;
        }

        var generator = Activator.CreateInstance(genTypes[genIndex]) as IGenerator;
        if (generator == null)
        {
            Console.WriteLine(Lang.Generate_Error_UnknownGenerator, settings.Generator);
            return 1;
        }

        //This runs the build.borz command in the current directory.
        Workspace.Init(Directory.GetCurrentDirectory());
        Core.Borz.GenerateWorkspace(generator);
        return 0;
    }
}