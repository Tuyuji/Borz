using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AkoSharp;
using ByteSizeLib;

namespace Borz.Linux;

public class LinuxPlatform : IPlatform
{
    public string GetUserConfigPath()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".config");

        return xdgConfigHome;
    }

    public void Init()
    {
        //add mingw
        var mingw = MachineInfo.NewOrGet("windows", "x86_64");
        mingw.Binaries = new()
        {
            {"gcc", "x86_64-w64-mingw32-gcc"},
            {"g++", "x86_64-w64-mingw32-g++"},
        };
        mingw.Compilers = new()
        {
            {"c", "gcc"},
            {"cpp", "gcc"}
        };
    }

    public MemoryInfo GetMemoryInfo()
    {
        var info = File.ReadAllText("/proc/meminfo");
        ByteSize? total = null, available = null;

        foreach (Match match in Regex.Matches(info, @"(?<key>\w+):\s+(?<value>\d+\s+\w+)"))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;
            if (key == "MemTotal")
                total = ByteSize.Parse(value);
            else if (key == "MemAvailable")
                available = ByteSize.Parse(value);
        }

        if (total == null || available == null)
            throw new Exception("Could not get memory info");

        return new MemoryInfo((ByteSize)total, (ByteSize)available);
    }
}