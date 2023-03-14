using System.Text.RegularExpressions;

namespace Borz.PkgConfig;

public static class PkgParser
{
    private static readonly string[] SearchLocations = new []
    {
        "/usr/local/lib/pkgconfig/",
        "/usr/lib/pkgconfig/",
        "/usr/lib64/pkgconfig/",
        "/usr/lib/x86_64-linux-gnu/pkgconfig/",
        "/usr/X11/lib/pkgconfig/",
        "/usr/share/pkgconfig/",
        "/usr/local/share/pkgconfig/",
        "/opt/local/share/pkgconfig/",
        "/opt/local/lib/pkgconfig/",
        "/opt/local/libdata/pkgconfig/",
        "/usr/local/libdata/pkgconfig/",
        "/usr/local/lib64/pkgconfig/",
        "/usr/local/share/pkgconfig/",
        "/usr/local/libdata/pkgconfig/",
    };

    private static string? FindPkg(string pkg)
    {
        var pkgName = pkg + ".pc";
        foreach (string searchLocation in SearchLocations)
        {
            var searchLoc = Path.Combine(searchLocation, pkgName);
            if (File.Exists(searchLoc))
                return searchLoc;
        }

        return null;
    }

    //${var} 
    private static Regex varMatch = new Regex("\\${[\\S]*}");
    
    private static void ParsePkg(string content)
    {
        var lines = content.Split('\n');
        var pkg = new Dictionary<string, string>();
        foreach (string line in lines)
        {
            if(line.StartsWith('#') || line == String.Empty)
                continue;

            var eqParts = line.Split('=');
            if (eqParts.Length == 2)
            {
                var key = eqParts[0];
                var value = eqParts[1];

                var nn = varMatch.Matches(value);
                if (nn.Count != 0)
                {
                    foreach (Match o in nn)
                    {
                        var varName = o.Value.Substring(2, o.Length - 3);
                        var varValue = pkg[varName];
                        if (varValue != "")
                        {
                            value = value.Replace(o.Value, varValue);
                        }
                    }
                }
                
                pkg.Add(key, value);
                continue;
            }

            var parts = line.Split(':');
            if (parts.Length >= 1)
            {
                var key = parts[0];
                if (parts.Length == 1)
                    pkg[key] = "";

                var value = parts[1];

                var nn = varMatch.Matches(value);
                if (nn.Count != 0)
                {
                    foreach (Match match in nn)
                    {
                        var varName = match.Value.Substring(2, match.Length - 3);
                        var varValue = pkg[varName];
                        if (varValue != "")
                            value = value.Replace(match.Value, varValue);
                    }
                }
                
                pkg.Add(key, value);
            }
        }
        
        Console.WriteLine("lol");
    }

    public static void GetPkg(string pkgName)
    {
        var pcFile = FindPkg(pkgName);
        var pcContent = File.ReadAllText(pcFile!);
        ParsePkg(pcContent);
    }
}