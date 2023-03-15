namespace Borz.PkgConfig;

[BorzUserData]
public enum VersionType : uint
{
    None = 0,
    GTOrEq = 1,
    LTOrEq = 2,
    Eq = 3,
}