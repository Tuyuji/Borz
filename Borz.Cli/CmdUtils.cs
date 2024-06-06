using AkoSharp;

namespace Borz.Cli;

public class CmdUtils
{
    public static void LoadWorkspaceSettings(Options opt)
    {
        if (File.Exists("borzsettings.ako"))
        {
            Deserializer.FromString(Borz.Config.GetLayer(ConfLevel.Workspace), File.ReadAllText("borzsettings.ako"));
    
            opt.ValidConfigs = Borz.Config.Get("configs")?.ArrayValue.ConvertAll(input => (string)input) ?? opt.ValidConfigs;
            opt.Config = opt.ValidConfigs[0];
        }
    }
    
}