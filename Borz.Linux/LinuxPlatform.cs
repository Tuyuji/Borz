using System.Text.RegularExpressions;
using AkoSharp;
using Borz.Core;
using Borz.Core.Platform;
using ByteSizeLib;

namespace Borz.Linux;

[ShortType("PlatLinux")]
public class LinuxPlatform : IPlatform
{
    public string GetUserConfigPath()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".config");

        return xdgConfigHome;
    }

    public MemoryInfo GetMemoryInfo()
    {
        //Do the same thing as above, but using Regex
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