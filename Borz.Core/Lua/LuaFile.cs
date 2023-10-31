using MoonSharp.Interpreter;

namespace Borz.Core.Lua;

[MoonSharpUserData]
public class LuaFile
{
    public static void delete(Script script, string file)
    {
        file = script.GetAbsolute(file);
        File.Delete(file);
    }

    public static bool exists(Script script, string file)
    {
        file = script.GetAbsolute(file);
        return File.Exists(file);
    }

    public static bool copy(Script script, string srcFile, string dest, bool overwrite = false)
    {
        //if dest is a directory, copy to that directory
        //if dest is a file, copy to that file
        //if dest does not exist, copy to that file

        srcFile = script.GetAbsolute(srcFile);
        dest = script.GetAbsolute(dest);

        if (!File.Exists(srcFile))
            throw new Exception($"Source file {srcFile} does not exist.");

        if (Directory.Exists(dest))
        {
            var fileName = Path.GetFileName(srcFile);
            dest = Path.Combine(dest, fileName);
            try
            {
                File.Copy(srcFile, dest, overwrite);
            }
            catch (IOException ioException)
            {
                if (ioException.Message.Contains("already exists"))
                    return false;
            }

            return true;
        }

        return false;
    }
}