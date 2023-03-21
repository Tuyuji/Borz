namespace Borz.Core.Languages.C;

/// <summary>
/// A generic package dependency for C/C++ projects.
/// This is used for referencing a project that doesnt compile like system libraries or pkgconfig.
/// </summary>
/// <param name="Libs"></param>
/// <param name="LibDirs"></param>
/// <param name="Defines"></param>
/// <param name="Includes"></param>
[BorzUserData]
public record PkgDep(
    string[] Libs,
    string[] LibDirs,
    IReadOnlyDictionary<string, string?> Defines,
    string[] Includes,
    bool RequiresRpath
);