using System.Reflection;

namespace Borz;


public interface IPlatform
{
    private static IPlatform? _instance;
    
    public static IPlatform Instance
    {
        get
        {
            if (_instance != null) return _instance;

            //Lets find a platform implementation
            Type? platformType = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(e => 
                    e.IsClass &&
                    e.GetInterfaces().Any(i => i == typeof(IPlatform)));
            
            if (platformType == null)
                throw new Exception("No class found that implements IPlatform.");
            
            _instance = Activator.CreateInstance(platformType) as IPlatform;
            if (_instance == null)
                throw new Exception($"Failed to create instance of {platformType}");
            
            return _instance;
        }
    }

    public MemoryInfo GetMemoryInfo();

    //This isn't for the borz folder, but for the user's config folder
    public string GetUserConfigPath();
}
