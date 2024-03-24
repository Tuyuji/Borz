using Borz.Cli.Commands;
using Spectre.Console.Cli;

Borz.Borz.Init();

var app = new CommandApp();
app.Configure(config =>
{
    config.Settings.ApplicationName = "Borz";
    config.AddCommand<CompileCommand>("compile").WithAlias("c");
});

var res = 1;
try
{
    res = app.Run(args);
}
catch (Exception ex)
{
    // Ignored
}

Borz.Borz.Shutdown();

return res;