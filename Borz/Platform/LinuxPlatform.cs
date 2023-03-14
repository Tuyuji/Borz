using System.Text.RegularExpressions;
using ByteSizeLib;

namespace Borz.Platform;

public class LinuxPlatform : IPlatform
{
    private static Dictionary<string, ByteSize>? _memoryInfo;

    public ByteSize GetTotalMemory()
    {
        return GetMemoryInfo()["MemTotal"];
    }

    public ByteSize GetFreeMemory()
    {
        return GetMemoryInfo()["MemFree"];
    }

    public ByteSize GetAvailableMemory()
    {
        return GetMemoryInfo()["MemAvailable"];
    }

    public string GetUserConfigPath()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return xdgConfigHome;
    }

    private static Dictionary<string, ByteSize> GetMemoryInfo()
    {
        if(_memoryInfo != null)
            return _memoryInfo;
        
        //Do the same thing as above, but using Regex
        var info = File.ReadAllText("/proc/meminfo");
        var dict = new Dictionary<string, ByteSize>();
        foreach (Match match in Regex.Matches(info, @"(?<key>\w+):\s+(?<value>\d+\s+\w+)"))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;
            if(ByteSize.TryParse(value, out var size))
                dict.Add(key, size);
        }
        
_memoryInfo = dict;
        return dict;
    }
}