using AkoSharp;
using Borz.PkgConfig;
using MoonSharp.Interpreter;

namespace Borz.Lua;

[BorzUserData]
public static class LuaPkgConf
{
    private static KeyValuePair<VersionType, string> ConvertVersionStringToPair(string input)
    {
        VersionType versionOp = VersionType.None;
        input = input.Replace(" ", null);
        if (input.StartsWith(">="))
        {
            versionOp = VersionType.GTOrEq;
            input = input[2..];
        }
        else if (input.StartsWith("<="))
        {
            versionOp = VersionType.LTOrEq;
            input = input[2..];
        }
        else if (input.StartsWith("="))
        {
            versionOp = VersionType.Eq;
            input = input[1..];
        }

        return new KeyValuePair<VersionType, string>(versionOp, input);
    }

    public static PkgConfigProject? Query(string name, bool required = true, string versionIn = "")
    {
        var (versionOp, version) = ConvertVersionStringToPair(versionIn);

        var pkg = PkgConfig.PkgConfig.GetPackage(name, versionOp, version);
        if (pkg == null && required)
        {
            throw new PackageNotFoundException(name, versionOp, version);
        }

        return pkg == null ? null : new PkgConfigProject(pkg);
    }

    public static IDictionary<string, PkgConfigProject?> FromAko(Script script, string akoFile)
    {
        var akoLoc = Util.GetAbsolute(script, akoFile);
        var ako = Deserializer.FromString(File.ReadAllText(akoLoc));
        if (ako.Type != AkoVar.VarType.TABLE)
            throw new Exception("Ako file must be a table");

        var dict = new Dictionary<string, PkgConfigProject?>();
        foreach (var (key, value) in ako.TableValue)
        {
            var pkgName = key;
            if (value.ContainsKey("name") && value["name"] != null)
                pkgName = (string)value["name"];

            VersionType versionOp = VersionType.None;
            string version = "";
            if (value.ContainsKey("version"))
            {
                var (op, ver) = ConvertVersionStringToPair((string)value["version"]);
                versionOp = op;
                version = ver;
            }

            var isRequired = false;
            if (value.ContainsKey("req"))
                isRequired = (bool)value["req"];

            var pkg = PkgConfig.PkgConfig.GetPackage(pkgName, versionOp, version);
            if (pkg == null && isRequired)
            {
                throw new PackageNotFoundException(pkgName, versionOp, version);
            }

            dict.Add(key, pkg == null ? null : new PkgConfigProject(pkg));
        }

        return dict;
    }
}

public sealed class PackageNotFoundException : Exception
{
    public static string GetMessage(string pkgName, VersionType type, string version)
    {
        string str = $"Required package {pkgName} not found";
        if (type != VersionType.None)
        {
            str += $", with version {version}";
            switch (type)
            {
                case VersionType.GTOrEq:
                    str += " or greater";
                    break;
                case VersionType.LTOrEq:
                    str += " or less";
                    break;
                case VersionType.Eq:
                    str += " exactly";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return str;
    }

    public PackageNotFoundException(string pkgName, VersionType type, string version)
        : base(GetMessage(pkgName, type, version))
    {
        MugiLog.Fatal(Message);
    }
}