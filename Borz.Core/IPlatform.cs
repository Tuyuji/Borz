namespace Borz.Core.Platform;

public interface IPlatform
{
    private static IPlatform? _instance;

    public static IPlatform Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var type = (Type)Borz.Config.Get("platform");
            _instance = (IPlatform?)Activator.CreateInstance(type);
            if (_instance == null)
                throw new Exception("Platform not supported");

            return _instance;
        }
    }

    public MemoryInfo GetMemoryInfo();

    //This isn't for the borz folder, but for the user's config folder
    public string GetUserConfigPath();
}