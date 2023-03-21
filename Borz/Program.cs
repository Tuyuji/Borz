using Borz.Cli;
using Spectre.Console.Cli;

namespace Borz;

static class Program
{
    public static int Main(string[] args)
    {
        Core.Borz.Init();
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.Settings.ApplicationName = "Borz";
            config.AddCommand<CompileCommand>("compile")
                .WithAlias("c");
            config.AddCommand<GenerateCommand>("generate")
                .WithAlias("g");
            config.AddBranch<ConfigSettings>("config", conf => { conf.AddCommand<ListConfigCommand>("list"); });
        });
        var res = app.Run(args);
        Core.Borz.Shutdown();

        return res;
    }
}