namespace Borz.Core;

public enum ConfLevel : int
{
    Defaults = 0, //Defaults in borz
    Platform = 1, //Platform specific defaults
    Script = 2, //Stuff from scripts
    UserGobal = 3, // Users global config e.g ~/.borz/config 
    Workspace = 4, // Workspace specific config e.g. .borz/config
    UserWorkspace = 5 // User specific project config e.g. .borz/config.ako
}