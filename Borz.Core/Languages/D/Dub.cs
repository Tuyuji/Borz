using Borz.Core.PkgConfig;

namespace Borz.Core.Languages.D;

[BorzUserData]
public class Dub
{
    public static string GetDubLocation()
    {
        //~/.dub/packages
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".dub");
    }

    public static string GetDubPkgLocation()
    {
        return Path.Combine(GetDubLocation(), "packages");
    }

    public static bool PackageExists(string name, VersionType op, string version)
    {
        var pkgLocation = GetDubPkgLocation();
        //folder structure is: name-version

        //get all folders in pkgLocation that start with name
        var dirs = Directory.GetDirectories(pkgLocation, $"{name}-*");
        if (dirs.Length == 0) return false;

        //find the correct version
        if (op != VersionType.Eq) throw new NotImplementedException();

        return dirs.Any(dir => dir.EndsWith(version));
    }
}