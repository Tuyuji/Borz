using Borz.Cli;
using Borz.Core;
using Spectre.Console.Cli;

namespace Borz;

internal static class Program
{
    public static int Main(string[] args)
    {
        Core.Borz.Init();
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.Settings.ApplicationName = "Borz";
            config.AddCommand<InitCommand>("init");
            config.AddCommand<CompileCommand>("compile")
                .WithAlias("c");
            config.AddCommand<RunCommand>("run");
            config.AddCommand<CleanCommand>("clean");
            config.AddCommand<GenerateCommand>("generate")
                .WithAlias("g");
            config.AddBranch<ConfigSettings>("config", conf => { conf.AddCommand<ListConfigCommand>("list"); });
        });
        var res = 1;
        try
        {
            res = app.Run(args);
            MugiLog.Wait();
        }
        catch (Exception e)
        {
            // ignored
        }

        Core.Borz.Shutdown();

        return res;
    }
}