using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AkoSharp;
using Borz.Lua;
using Spectre.Console.Cli;

namespace Borz.Cli.Commands;

[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class GenerateCommand : Command<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<generator>")] public string Generator { get; set; }
        
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
        };

        if (settings.Target != null)
        {
            var parsedTarget = MachineInfo.Parse(settings.Target);
            if (parsedTarget == null)
            {
                MugiLog.Error($"Unknown target: {settings.Target} known targets are:");
                foreach (var machine in MachineInfo.GetKnownMachines())
                {
                    MugiLog.Error(machine.ToString());
                }
                
                return 1;
            }
            
            opt.Target = parsedTarget;
        }
        
        CmdUtils.LoadWorkspaceSettings(opt);
        
        if (settings.Config != null)
        {
            if (!opt.ValidConfigs.Contains(settings.Config))
            {
                MugiLog.Error($"{settings.Config} isn't valid.");
                return 1;
            }
            
            opt.Config = settings.Config;
        }
        
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

        var generator = GeneratorFactory.TryGetGenerator(settings.Generator);
        if (generator == null)
        {
            MugiLog.Error($"No generator found for {settings.Generator}");
            return 1;
        }
        
        try
        {
            var (success, error) = generator.Generate(ws, opt);
            if (!success)
            {
                MugiLog.Error(error);
                return 1;
            }
        }
        catch (Exception ex)
        {
            // Ignore
            Log.error(ex.ToString());
            return 1;
        }

        return 0;
    }

    
}