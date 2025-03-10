using System.IO.Compression;
using System.Net;
using MoonSharp.Interpreter;

namespace Borz.Lua;

[MoonSharpUserData]
public class Utils
{
    [MoonSharpHidden]
    public static string? GetBorzScriptFilePath(string directory)
    {
        //See if directory has build.borz first then borz.lua
        var borzFile = Path.Combine(directory, "build.borz");
        if (!File.Exists(borzFile))
        {
            borzFile = Path.Combine(directory, "borz.lua");
            if (!File.Exists(borzFile))
                return null;
        }

        return borzFile;
    }

    public static string removeFromString(string input, string match)
    {
        return input.Replace(match, String.Empty);
    }

    public static string AddMachineIfixsToFileName(string filename, BinType binType, MachineInfo info)
    {
        switch (binType)
        {
            case BinType.WindowsApp:
            case BinType.ConsoleApp:
                return $"{info.ExePrefix}{filename}{info.ExeExt}";
            case BinType.SharedObj:
                return $"{info.SharedLibPrefix}{filename}{info.SharedLibExt}";
            case BinType.StaticLib:
                return $"{info.StaticLibPrefix}{filename}{info.StaticLibExt}";
            default:
                throw new ArgumentOutOfRangeException(nameof(binType), binType, null);
        }
    }
    
    public static DynValue runCmd(Script script, string cmd, string args, string? workingdir = null, IDictionary<string, string?>? env = null)
    {
        var realworkingdir = script.GetCwd();
        if (workingdir != null)
            realworkingdir = workingdir;

        ProcUtil.RunOutput result;
        result = ProcUtil.RunCmd(cmd, args, realworkingdir, env);
       
        var tuple = DynValue.NewTuple(DynValue.FromObject(script, result.Exitcode),
            DynValue.FromObject(script, result.Ouput), DynValue.FromObject(script, result.Error));
        return tuple;
    }
    
    //https://stackoverflow.com/a/3822913
    //TODO: Handle links
    [MoonSharpHidden]
    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        //Now Create all of the directories
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

        //Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }
    
    public static bool ends_with(string input, string end)
    {
        return input.EndsWith(end);
    }

    public static bool starts_with(string input, string start)
    {
        return input.StartsWith(start);
    }

    public static void sleep(double time)
    {
        Thread.Sleep(TimeSpan.FromSeconds(time));
    }
    
    public enum ResourceType : uint
    {
        Archive = 0
    }

    /// <summary>
    /// Download a needed resource from the internet.
    /// </summary>
    /// <param name="script">The script thats performing this action.</param>
    /// <param name="resourceType">What type of resource.</param>
    /// <param name="url">Uri to the resource.</param>
    /// <param name="extractTo">Relative location to extract to.</param>
    /// <param name="fileToCheck">If the given file exists then skip. Path is relative</param>
    /// <param name="folderToExtract">Path in archive to copy from.</param>
    /// <returns>False if a failure happened, only failure.</returns>
    public static bool getResource(Script script, ResourceType resourceType, string url, string extractTo,
        string fileToCheck = "", string folderToExtract = "")
    {
        if (File.Exists(Path.Combine(script.GetCwd(), fileToCheck)))
            return true;

        if (Environment.OSVersion.Platform != PlatformID.Unix)
            return false;

        if (string.IsNullOrWhiteSpace(extractTo))
            throw ScriptRuntimeException.BadArgument(2, "GetResource", "extractTo is empty.");

        extractTo = Path.Combine(script.GetCwd(), extractTo);

        var uri = new Uri(url);
        var filename = uri.LocalPath.Split('/').Last();

        var borzTemp = Path.Combine(Path.GetTempPath(), "borz");
        if (!Directory.Exists(borzTemp))
            Directory.CreateDirectory(borzTemp);

        var outputLocation = Path.Combine(borzTemp, filename);

        // using (var dlStat = ConsoleHandler.AddStatus("Downloading " + filename))
        // {
        if (!File.Exists(outputLocation))
            //Doesnt exist, download the resource
            try
            {
                MugiLog.Info("Downloading " + filename);
                using WebClient wc = new();
                wc.DownloadFile(url, outputLocation);
            }
            catch (Exception)
            {
                return false;
            }
        // }

        //Got the file, now extract it
        var exDir = Path.Combine(Path.GetTempPath(), "borz-" + Path.GetRandomFileName());
        Directory.CreateDirectory(exDir);

        if (resourceType != ResourceType.Archive)
            return false;

        var supportedExtensions = new[]
        {
            ".tar.gz",
            ".zip"
        };

        if (!supportedExtensions.Any(x => filename.EndsWith(x)))
            return false;

        if (filename.EndsWith(".tar.gz"))
        {
            var extractResult = ProcUtil.RunCmd("tar", $"-xf {outputLocation} -C {exDir}");
            if (extractResult.Exitcode != 0)
                return false;
        }
        else if (filename.EndsWith(".zip"))
        {
            //Unzip using C#
            try
            {
                ZipFile.ExtractToDirectory(outputLocation, exDir);
            }
            catch (Exception ex)
            {
                MugiLog.Error(ex.Message);
                return false;
            }
        }

        var folderToCopy = string.Empty;

        //Find the folder to copy
        folderToCopy = folderToExtract == "" ? exDir : Path.Combine(exDir, folderToExtract);

        //Get all files and folders from folderToCopy 
        var files = Directory.GetFileSystemEntries(folderToCopy, "*", SearchOption.TopDirectoryOnly);

        //Copy all files and folders to extractTo
        foreach (var file in files)
            //See if file is a directory or file
            if (Directory.Exists(file))
                //Its a directory, copy it
                CopyFilesRecursively(file, Path.Combine(extractTo, Path.GetFileName(file)));
            else
                //Its a file, copy it
                File.Copy(file, Path.Combine(extractTo, Path.GetFileName(file)));

        //Clean up
        Directory.Delete(exDir, true);


        return true;
    }
    
    public static bool downloadFile(Script script, string url, string outputLocation)
    {
        try
        {
            outputLocation = script.GetAbsolute(outputLocation);
            using WebClient wc = new();
            wc.DownloadFile(url, outputLocation);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static string? conf_get(params string[] keys)
    {
        var value = Borz.Config.Get(keys);
        return value == null ? null : (string?)value.Value.ToString();
    }

    public static int getMaxJobs()
    {
        return Borz.GetUsableThreadCount();
    }
}