using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpUserData]
public class Util
{
    //private static Dictionary<Guid, ConsoleHandler.ConsoleStatHandle> _statHandles = new();

    public static void Sleep(uint ms)
    {
        System.Threading.Thread.Sleep((int)ms);
    }

    public static DynValue RunCmd(Script script, string cmd, string args)
    {
        var result = UnixUtil.RunCmd(cmd, args, script.GetCwd());
        var tuple = DynValue.NewTuple(DynValue.FromObject(script, result.Exitcode),
            DynValue.FromObject(script, result.Ouput), DynValue.FromObject(script, result.Error));
        return tuple;
    }

    public static string path_combine(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public static string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public static string GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public static string GetAbsolute(Script script, string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(script.GetCwd(), path);
        return path;
    }

    public static Platform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Platform.Linux;
        }

        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Platform.MacOS;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Platform.Windows;
        }

        return Platform.Unknown;
    }

    [BorzUserData]
    public enum ResourceType : uint
    {
        Archive = 0,
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
    public static bool GetResource(Script script, ResourceType resourceType, string url, string extractTo,
        string fileToCheck = "", string folderToExtract = "")
    {
        if (File.Exists(Path.Combine(script.GetCwd(), fileToCheck)))
            return true;

        if (Environment.OSVersion.Platform != PlatformID.Unix)
            return false;

        if (string.IsNullOrWhiteSpace(extractTo))
        {
            throw ScriptRuntimeException.BadArgument(2, "GetResource", "extractTo is empty.");
        }

        extractTo = Path.Combine(script.GetCwd(), extractTo);

        Uri uri = new Uri(url);
        var filename = uri.LocalPath.Split('/').Last();

        string borzTemp = Path.Combine(Path.GetTempPath(), "borz");
        if (!Directory.Exists(borzTemp))
            Directory.CreateDirectory(borzTemp);

        string outputLocation = Path.Combine(borzTemp, filename);

        // using (var dlStat = ConsoleHandler.AddStatus("Downloading " + filename))
        // {
        if (!File.Exists(outputLocation))
        {
            //Doesnt exist, download the resource
            try
            {
                using WebClient wc = new();
                wc.DownloadFile(url, outputLocation);
            }
            catch (Exception)
            {
                return false;
            }
        }
        // }

        //Got the file, now extract it
        string exDir = Path.Combine(Path.GetTempPath(), "borz-" + Path.GetRandomFileName());
        Directory.CreateDirectory(exDir);

        if (resourceType != ResourceType.Archive)
            return false;

        string[] supportedExtensions = new[]
        {
            ".tar.gz",
            ".zip"
        };

        if (!supportedExtensions.Any(x => filename.EndsWith(x)))
            return false;

        if (filename.EndsWith(".tar.gz"))
        {
            var extractResult = UnixUtil.RunCmd("tar", $"-xf {outputLocation} -C {exDir}");
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

        string folderToCopy = string.Empty;

        //Find the folder to copy
        folderToCopy = folderToExtract == "" ? exDir : Path.Combine(exDir, folderToExtract);

        //Get all files and folders from folderToCopy 
        var files = Directory.GetFileSystemEntries(folderToCopy, "*", SearchOption.TopDirectoryOnly);

        //Copy all files and folders to extractTo
        foreach (var file in files)
        {
            //See if file is a directory or file
            if (Directory.Exists(file))
            {
                //Its a directory, copy it
                CopyFilesRecursively(file, Path.Combine(extractTo, Path.GetFileName(file)));
            }
            else
            {
                //Its a file, copy it
                File.Copy(file, Path.Combine(extractTo, Path.GetFileName(file)));
            }
        }

        //Clean up
        Directory.Delete(exDir, true);


        return true;
    }


    //https://stackoverflow.com/a/3822913
    //TODO: Handle links
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    #region Directory Functions

    public static bool Mkdir(Script script, string dir)
    {
        dir = GetAbsolute(script, dir);
        return Directory.CreateDirectory(dir).Exists;
    }

    public static bool DirExists(Script script, string dir)
    {
        dir = GetAbsolute(script, dir);
        return Directory.Exists(dir);
    }

    public static void RmDir(Script script, string dir)
    {
        dir = GetAbsolute(script, dir);
        Directory.Delete(dir, true);
    }

    //List dirs only list top level directories
    public static string[] ListDirs(Script script, string dir)
    {
        dir = GetAbsolute(script, dir);
        return Directory.GetDirectories(dir);
    }

    public static string[] ListDirs(Script script)
    {
        return Directory.GetDirectories(script.GetCwd());
    }

    #endregion

    #region File Functions

    public static void RmFile(Script script, string file)
    {
        file = GetAbsolute(script, file);
        File.Delete(file);
    }

    public static bool FileExists(Script script, string file)
    {
        file = GetAbsolute(script, file);
        return File.Exists(file);
    }

    public static bool DownloadFile(Script script, string url, string outputLocation)
    {
        try
        {
            outputLocation = GetAbsolute(script, outputLocation);
            using WebClient wc = new();
            wc.DownloadFile(url, outputLocation);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion
}