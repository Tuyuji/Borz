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
    
    public static MachineInfo ParseMachine(string input)
    {
        var machineInfo = MachineInfo.Parse(input, MachineInfo.GetCurrentMachineInfo());

        if (machineInfo == null)
            throw new Exception("Cant parse given machine info.");

        var machinesPath = Path.Combine(IPlatform.Instance.GetUserConfigPath(), "borz", "machines.ako");
        if (!File.Exists(machinesPath))
            throw new Exception("Couldn't fine machines.ako");
    
        var machines = Deserializer.FromString(File.ReadAllText(machinesPath));
        if (machines == null)
            throw new Exception("Failed to parse machines.ako");

        if (!machines.TryGet(machineInfo.OS, out var osTable))
        {
            throw new Exception($"OS \"{machineInfo.OS}\" not in machines.ako");
        }

        if (!osTable.TryGet(machineInfo.Arch, out var archTable))
        {
            throw new Exception($"Arch \"{machineInfo.Arch}\" not found in {machineInfo.OS}");
        }

        if (archTable.TryGet("compilers", out var compilers))
        {
            foreach (var akoVar in compilers.TableValue)
            {
                machineInfo.Compilers.Add(akoVar.Key, akoVar.Value);
            }
        }

        if (archTable.TryGet("bins", out var bins))
        {
            foreach (var var in bins.TableValue)
            {
                machineInfo.Binaries.Add(var.Key, var.Value);
            }
        }

        return machineInfo;
    }
}