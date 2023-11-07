using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Borz.Core.Helpers;

public class CompileCommands
{
    //Either arguments or command is required.
    //arguments is preferred, as shell (un)escaping is a possible source of errors.
    [Serializable]
    public class CompileCommand
    {
        //The working directory of the compilation.
        //All paths specified in the command or file fields must be either absolute or relative to this directory.
        [JsonPropertyName("directory")] public string Directory { get; set; } = "";

        //The main translation unit source processed by this compilation step.
        //This is used by tools as the key into the compilation database.
        //There can be multiple command objects for the same file,
        //for example if the same source file is compiled with different configurations.
        [JsonPropertyName("file")] public string File { get; set; } = "";

        //The compile command argv as list of strings.
        //This should run the compilation step for the translation unit file.
        //arguments[0] should be the executable name, such as clang++.
        //Arguments should not be escaped, but ready to pass to execvp().
        [JsonPropertyName("arguments")] public string[] Arguments { get; set; } = Array.Empty<string>();

        //The compile command as a single shell-escaped string.
        //Arguments may be shell quoted and escaped following platform conventions,
        //with ‘"’ and ‘\’ being the only special characters.
        //Shell expansion is not supported.
        [JsonPropertyName("command")] public string Command { get; set; } = "";

        //The name of the output created by this compilation step.
        //This field is optional.
        //It can be used to distinguish different processing modes of the same input file.
        [JsonPropertyName("output")] public string Output { get; set; } = "";
    }

    public class CompileDatabase
    {
        private ConcurrentBag<CompileCommand> _commands = new();

        public CompileDatabase(string file = "")
        {
            if (file != "" && File.Exists(file)) LoadFromFile(file);
        }

        public void Add(CompileCommand cmd)
        {
            _commands.Add(cmd);
        }

        private void LoadFromFile(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException("Compile database file not found.", file);

            var json = JsonSerializer.Deserialize<List<CompileCommand>>(File.ReadAllText(file));
            if (json != null) _commands = new ConcurrentBag<CompileCommand>(json);
        }

        public void SaveToFile(string file)
        {
            File.WriteAllText(file, JsonSerializer.Serialize(_commands));
        }
    }
}