namespace Borz.Languages.C;

public class PosixDepParser
{
    public static Dictionary<string, List<string>> Parse(string src)
    {
        var dependencies = new Dictionary<string, List<string>>();
        var currentTarget = "";

        string[] lines = src.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.EndsWith(":") || line.EndsWith(": \\"))
            {
                currentTarget = line.TrimEnd('\\').Trim().TrimEnd(':').Trim();
                dependencies[currentTarget] = new List<string>();
            }
            else
            {
                var depFiles = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).First();
                dependencies[currentTarget].Add(depFiles);
            }
        }

        return dependencies;
    }
}