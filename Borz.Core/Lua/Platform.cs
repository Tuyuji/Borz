namespace Borz.Core.Lua;

[BorzUserData]
public enum Platform : int
{
    Unknown = 1,
    Linux = 2,
    MacOS = 3,
    Windows = 4,
    Android = 5,
    iOS = 6,
    Webassembly = 7,
}