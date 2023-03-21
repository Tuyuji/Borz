namespace Borz.Core;

public enum ConfLevel : int
{
    Defaults = 0, //Defaults in borz
    Platform = 1, //Platform specific defaults
    UserGobal = 2, // Users global config e.g ~/.borz/config 
    Workspace = 3, // Workspace specific config e.g. .borz/config
    UserWorkspace = 4, // User specific project config e.g. .borz/config.ako
}