using Newtonsoft.Json;

namespace Borz.Core.Generators;

[FriendlyName("Sublime")]
public class SublimeGenerator : IGenerator
{
    [Serializable]
    private class SublimeProject
    {
        [JsonProperty("folders")] public List<SublimeFolder> Folders { get; set; }

        [JsonProperty("settings")] public SublimeSettings Settings { get; set; }

        public SublimeProject()
        {
            Folders = new List<SublimeFolder>();
        }
    }

    [Serializable]
    private class SublimeFolder
    {
        [JsonProperty("name")] public string? Name { get; set; }

        [JsonProperty("path")] public string Path { get; set; }

        [JsonProperty("file_include_patterns")]
        public List<string>? FileIncludePatterns { get; set; }

        [JsonProperty("file_exclude_patterns")]
        public List<string>? FileExcludePatterns { get; set; }

        [JsonProperty("folder_include_patterns")]
        public List<string>? FolderIncludePatterns { get; set; }

        [JsonProperty("folder_exclude_patterns")]
        public List<string>? FolderExcludePatterns { get; set; }

        [JsonProperty("binary_file_patterns")] public List<string>? BinaryFilePatterns { get; set; }

        [JsonProperty("index_include_patterns")]
        public List<string>? IndexIncludePatterns { get; set; }

        [JsonProperty("index_exclude_patterns")]
        public List<string>? IndexExcludePatterns { get; set; }

        [JsonProperty("follow_symlinks")] public bool? FollowSymlinks { get; set; }

        public SublimeFolder()
        {
        }
    }

    [Serializable]
    private class SublimeSettings
    {
        [JsonProperty("tab_size")] public int TabSize { get; set; }
    }


    //Check this url for more info on build systems:
    //https://www.sublimetext.com/docs/build_systems.html
    [Serializable]
    private class SublimeBuildTool
    {
        [JsonProperty("selector")] public string? Selector { get; set; }

        [JsonProperty("file_patterns")] public List<string>? FilePatterns { get; set; }

        [JsonProperty("keyfiles")] public List<string>? KeyFiles { get; set; }

        [JsonProperty("variants")] public List<SublimeBuildTool> Variants { get; set; }

        //next is cancel, this can be a string or a list of strings
        [JsonProperty("cancel")] public object? Cancel { get; set; }

        [JsonProperty("target")] public string? Target { get; set; }

        [JsonProperty("windows")] public object? Windows { get; set; }

        [JsonProperty("osx")] public object? OSX { get; set; }

        [JsonProperty("linux")] public object? Linux { get; set; }

        [JsonProperty("cmd")] public string? Command { get; set; }

        [JsonProperty("shell_cmd")] public string? ShellCommand { get; set; }

        [JsonProperty("working_dir")] public string? WorkingDirectory { get; set; }

        [JsonProperty("file_regex")] public string? FileRegex { get; set; }

        [JsonProperty("line_regex")] public string? LineRegex { get; set; }

        [JsonProperty("encoding")] public string? Encoding { get; set; }

        [JsonProperty("env")] public Dictionary<string, string>? Environment { get; set; }

        [JsonProperty("quiet")] public bool? Quiet { get; set; }

        [JsonProperty("word_wrap")] public bool? WordWrap { get; set; }

        [JsonProperty("syntax")] public string? Syntax { get; set; }
    }


    //No idea how sublime likes reused paths, so we'll just generate a new one each time
    //BDF = Borz Dummy Folder :)
    int dummyPathCounter = 0;

    private string GenerateDummyPath()
    {
        dummyPathCounter++;
        return $"$BDF{dummyPathCounter}";
    }

    public void Generate()
    {
        var wsName = Workspace.Settings.Name;
        SublimeProject project = new SublimeProject();

        //go though workspace projects
        foreach (var workspaceProject in Workspace.Projects)
        {
            //for now just add the project directory
            SublimeFolder folder = new SublimeFolder();
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
        var file = new System.IO.StreamWriter(
            Path.Combine(Workspace.Location, $"{wsName}.sublime-project"),
            new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write });
        //Json comment to tell the user not to edit the file
        file.WriteLine("//This file is generated by Borz. Do not edit this file.");

        file.WriteLine(JsonConvert.SerializeObject(project, Formatting.Indented, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        }));
        file.Close();
    }
}