using System.Text.Json;
using System.Text.Json.Serialization;

namespace Borz.Core.Generators;

[FriendlyName("Sublime")]
public class SublimeGenerator : IGenerator
{
    [Serializable]
    private class SublimeProject
    {
        [JsonPropertyName("folders")] public List<SublimeFolder> Folders { get; set; }

        [JsonPropertyName("settings")] public SublimeSettings Settings { get; set; }

        public SublimeProject()
        {
            Folders = new List<SublimeFolder>();
        }
    }

    [Serializable]
    private class SublimeFolder
    {
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("path")] public string Path { get; set; }

        [JsonPropertyName("file_include_patterns")]
        public List<string>? FileIncludePatterns { get; set; }

        [JsonPropertyName("file_exclude_patterns")]
        public List<string>? FileExcludePatterns { get; set; }

        [JsonPropertyName("folder_include_patterns")]
        public List<string>? FolderIncludePatterns { get; set; }

        [JsonPropertyName("folder_exclude_patterns")]
        public List<string>? FolderExcludePatterns { get; set; }

        [JsonPropertyName("binary_file_patterns")]
        public List<string>? BinaryFilePatterns { get; set; }

        [JsonPropertyName("index_include_patterns")]
        public List<string>? IndexIncludePatterns { get; set; }

        [JsonPropertyName("index_exclude_patterns")]
        public List<string>? IndexExcludePatterns { get; set; }

        [JsonPropertyName("follow_symlinks")] public bool? FollowSymlinks { get; set; }

        public SublimeFolder()
        {
        }
    }

    [Serializable]
    private class SublimeSettings
    {
        [JsonPropertyName("tab_size")] public int TabSize { get; set; }
    }


    //Check this url for more info on build systems:
    //https://www.sublimetext.com/docs/build_systems.html
    [Serializable]
    private class SublimeBuildTool
    {
        [JsonPropertyName("selector")] public string? Selector { get; set; }

        [JsonPropertyName("file_patterns")] public List<string>? FilePatterns { get; set; }

        [JsonPropertyName("keyfiles")] public List<string>? KeyFiles { get; set; }

        [JsonPropertyName("variants")] public List<SublimeBuildTool> Variants { get; set; }

        //next is cancel, this can be a string or a list of strings
        [JsonPropertyName("cancel")] public object? Cancel { get; set; }

        [JsonPropertyName("target")] public string? Target { get; set; }

        [JsonPropertyName("windows")] public object? Windows { get; set; }

        [JsonPropertyName("osx")] public object? OSX { get; set; }

        [JsonPropertyName("linux")] public object? Linux { get; set; }

        [JsonPropertyName("cmd")] public string? Command { get; set; }

        [JsonPropertyName("shell_cmd")] public string? ShellCommand { get; set; }

        [JsonPropertyName("working_dir")] public string? WorkingDirectory { get; set; }

        [JsonPropertyName("file_regex")] public string? FileRegex { get; set; }

        [JsonPropertyName("line_regex")] public string? LineRegex { get; set; }

        [JsonPropertyName("encoding")] public string? Encoding { get; set; }

        [JsonPropertyName("env")] public Dictionary<string, string>? Environment { get; set; }

        [JsonPropertyName("quiet")] public bool? Quiet { get; set; }

        [JsonPropertyName("word_wrap")] public bool? WordWrap { get; set; }

        [JsonPropertyName("syntax")] public string? Syntax { get; set; }
    }


    //No idea how sublime likes reused paths, so we'll just generate a new one each time
    //BDF = Borz Dummy Folder :)
    private int dummyPathCounter = 0;

    private string GenerateDummyPath()
    {
        dummyPathCounter++;
        return $"$BDF{dummyPathCounter}";
    }

    public void Generate()
    {
        var wsName = Workspace.Settings.Name;
        var project = new SublimeProject();

        //go though workspace projects
        foreach (var workspaceProject in Workspace.Projects)
        {
            //for now just add the project directory
            var folder = new SublimeFolder();
            folder.Name = workspaceProject.Name;
            //get the relative path to the project directory from the workspace directory
            folder.Path = Path.GetRelativePath(Workspace.Location,
                workspaceProject.GetPathAbs(workspaceProject.ProjectDirectory));

            project.Folders.Add(folder);
        }

        var wpRootStr = "Workspace Root";
        //spacer
        project.Folders.Add(new SublimeFolder()
            { Name = new string('-', wpRootStr.Length + 7), Path = GenerateDummyPath() });
        project.Folders.Add(new SublimeFolder() { Name = wpRootStr, Path = "." });


        //write the project file to .sublime-project
        var file = new StreamWriter(
            Path.Combine(Workspace.Location, $"{wsName}.sublime-project"),
            new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write });
        //Json comment to tell the user not to edit the file
        file.WriteLine("//This file is generated by Borz. Do not edit this file.");

        file.WriteLine(JsonSerializer.Serialize(project, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Always,
            WriteIndented = true
        }));
        file.Close();
    }
}