using AkoSharp;
using Borz.Core.Languages.C;
using Borz.Core.PkgConfig;
using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

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

    private static PkgDep ConvertPkgConfigInfoToPkgDep(PkgConfigInfo info)
    {
        var libPaths = new List<string>();
        var libs = new List<string>();
        var includePaths = new List<string>();
        var defines = new Dictionary<string, string?>();
        foreach (var flag in info.CFlags)
        {
            if (flag.StartsWith("-L"))
            {
                libPaths.Add(flag[2..]);
            }
            else if (flag.StartsWith("-I"))
            {
                includePaths.Add(flag[2..]);
            }
            else if (flag.StartsWith("-D"))
            {
                var define = flag[2..];
                var split = define.Split('=');
                if (split.Length == 2)
                {
                    defines[split[0]] = split[1];
                }
                else
                {
                    defines[split[0]] = null;
                }
            }
        }

        foreach (var lib in info.Libs)
        {
            if (lib.StartsWith("-l"))
                libs.Add(lib[2..]);
        }


        return new PkgDep(libs.ToArray(), libPaths.ToArray(), defines, includePaths.ToArray(), false);
    }

    public static PkgDep? Query(string name, bool required = true, string versionIn = "")
    {
        var (versionOp, version) = ConvertVersionStringToPair(versionIn);

        var pkg = PkgConfig.PkgConfig.GetPackage(name, versionOp, version);
        if (pkg == null && required)
        {
            throw new PackageNotFoundException(name, versionOp, version);
        }

        return pkg == null ? null : ConvertPkgConfigInfoToPkgDep(pkg);
    }

    public static IDictionary<string, PkgDep?> FromAko(Script script, string akoFile)
    {
        var akoLoc = Util.GetAbsolute(script, akoFile);
        var ako = Deserializer.FromString(File.ReadAllText(akoLoc));
        if (ako.Type != AkoVar.VarType.TABLE)
            throw new Exception("Ako file must be a table");

        var dict = new Dictionary<string, PkgDep?>();
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

            dict.Add(key, pkg == null ? null : ConvertPkgConfigInfoToPkgDep(pkg));
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