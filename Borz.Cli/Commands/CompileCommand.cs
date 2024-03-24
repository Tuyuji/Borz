using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AkoSharp;
using Borz.Lua;
using Spectre.Console.Cli;

namespace Borz.Cli.Commands;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class CompileCommand : Command<CompileCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [LocalDesc("Compile.Desc.Simulate")]
        [CommandOption("-n|--just-log")]
        [DefaultValue(false)]
        public bool Simulate { get; init; }

        [LocalDesc("Compile.Desc.Target")]
        [CommandOption("-t|--target")]
        [DefaultValue(null)]
        public string? Target { get; init; } = null;

        [CommandArgument(0, "[config]")] public string? Config { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var opt = new Options()
        {
            JustPrint = settings.Simulate
        };

        if (settings.Target != null)
        {
            var parsedTarget = CmdUtils.ParseMachine(settings.Target);
            opt.Target = parsedTarget;
        }
        
        CmdUtils.LoadWorkspaceSettings(opt);
        
        var ws = new Workspace(".");

        var script = ScriptRunner.CreateScript();
        script.Globals["ws"] = ws;
        script.Globals["opt"] = opt;

        var scriptPath = Utils.GetBorzScriptFilePath(ws.Location);
        if (!File.Exists(scriptPath))
        {
            MugiLog.Error($"No borz script found in {ws.Location}, need build.borz or borz.lua in root directory");
            return 1;
        }

        ScriptRunner.Eval(script, scriptPath);

        try
        {
            ws.Compile(opt);
        }
        catch (Exception ex)
        {
            // Ignore
            return 1;
        }

        return 0;
    }

    
}