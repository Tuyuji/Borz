using System.Diagnostics;
using Antlr4.Runtime;
using Borz.Cli;
using Spectre.Console.Cli;

namespace Borz;

static class Program
{
    public static int Main(string[] args)
    {
        MugiLog.Init();
        
        Utils.Init();
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.Settings.ApplicationName = "Borz";
            config.AddCommand<CompileCommand>("compile")
                .WithAlias("c");
            config.AddCommand<GenerateCommand>("generate")
                .WithAlias("g");
            config.AddBranch<ConfigSettings>("config", conf =>
            {
                conf.AddCommand<ListConfigCommand>("list");
            });
        });
        var res = app.Run(args);
        MugiLog.Shutdown();
        return res;
    }
    
}