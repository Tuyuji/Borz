using Borz.Languages.C;
using Borz.PkgConfig;

namespace Borz.Generators;

public class CMakeGenerator : IGenerator
{
    private static string StrListToCMake(List<string> list)
    {
        var str = "";
        foreach (var element in list)
        {
            str += element + "\n";
        }

        return str;
    }

    private static string WarningText =
        "#!!THIS FILE WAS AUTO-GENERATED.!!\n#Do not edit this file.\n#Edit the build.borz file instead.\n#Run 'borz generate' to update this file.";

    private static string CMakeMinVersion = "cmake_minimum_required(VERSION 3.20)\n";

    public void Generate()
    {
        //We need to make cmake lists for each project.
        //Each project has a cmake list thats called {ProjectName}.cmake
        //We make a cmake list in the workspace directory that includes all the project cmake lists.

        var file = new System.IO.StreamWriter(Path.Combine(Workspace.Location, "CMakeLists.txt"),
            new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write });
        file.WriteLine(WarningText);
        file.WriteLine(CMakeMinVersion);

        foreach (var project in Workspace.Projects)
        {
            if (project is CppProject cppProject)
                GenerateProject(cppProject, ref file);
            else if (project is CProject cProject)
                GenerateProject(cProject, ref file);
        }


        file.Close();
    }

    private string ProjectFileToAbsolute(Project project, string file)
    {
        var projectPath = project.ProjectDirectory;
        //make sure the path isn't absolute so we can make it absolute
        if (Path.IsPathRooted(file))
        {
            return file;
        }

        return Path.GetRelativePath(Workspace.Location, Path.GetFullPath(file, projectPath));
    }

    private List<string> ProjectFilesToAbsolute(Project project, List<string> files)
    {
        //copy files
        var filesCopy = new string[files.Count];

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            filesCopy[i] = ProjectFileToAbsolute(project, file);
        }

        return filesCopy.ToList();
    }

    private void GenerateProject(CProject project, ref StreamWriter file)
    {
        file.WriteLine("project(" + project.Name + ")\n");

        switch (project.Type)
        {
            case BinType.WindowsApp:
            case BinType.ConsoleApp:
                file.WriteLine(
                    $"add_executable({project.Name} {StrListToCMake(ProjectFilesToAbsolute(project, project.SourceFiles))})");
                break;
            case BinType.SharedObj:
                file.WriteLine(
                    $"add_library({project.Name} SHARED {StrListToCMake(ProjectFilesToAbsolute(project, project.SourceFiles))})");
                break;
            case BinType.StaticLib:
                file.WriteLine(
                    $"add_library({project.Name} STATIC {StrListToCMake(ProjectFilesToAbsolute(project, project.SourceFiles))})");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //setup headers
        if (project.PublicIncludePaths.Count > 0)
            file.WriteLine(
                $"target_include_directories({project.Name} PUBLIC {StrListToCMake(ProjectFilesToAbsolute(project, project.PublicIncludePaths))})");

        if (project.PrivateIncludePaths.Count > 0 || project.Dependencies.Count > 0)
        {
            file.WriteLine(
                $"target_include_directories({project.Name} PRIVATE {StrListToCMake(ProjectFilesToAbsolute(project, project.PrivateIncludePaths))}");

            foreach (var dependency in project.Dependencies)
                if (dependency is PkgConfigProject pkg)
                    foreach (var pkgIncludePath in pkg.IncludePaths)
                        file.WriteLine(pkgIncludePath);

            file.WriteLine(")");
        }

        //setup defines
        if (project.Defines.Count > 0 || project.Dependencies.Count > 0)
        {
            file.WriteLine($"target_compile_definitions({project.Name} PUBLIC ");
            foreach (var define in project.Defines)
            {
                file.WriteLine(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");
            }

            foreach (var dependency in project.Dependencies)
                if (dependency is PkgConfigProject pkg)
                    foreach (var define in pkg.Defines)
                        file.WriteLine(define.Value == null ? $"-D{define.Key}" : $"-D{define.Key}={define.Value}");

            file.WriteLine(")");
        }

        //setup library paths
        if (project.LibraryPaths.Count > 0 || project.Dependencies.Count > 0)
        {
            file.WriteLine("target_link_directories(" + project.Name + " PUBLIC ");
            file.WriteLine(StrListToCMake(ProjectFilesToAbsolute(project, project.LibraryPaths)));
            foreach (var dependency in project.Dependencies)
                if (dependency is PkgConfigProject pkg)
                    foreach (var libraryPath in pkg.LibraryPaths)
                        file.WriteLine(libraryPath);

            file.WriteLine(")");
        }

        //setup libraries
        if (project.Links.Count > 0 || project.Dependencies.Count > 0)
        {
            file.WriteLine("target_link_libraries(" + project.Name + " PUBLIC ");
            foreach (var library in project.Links)
            {
                file.WriteLine(library);
            }

            foreach (var projectDependency in project.Dependencies)
            {
                if (projectDependency is PkgConfigProject pkgConfigProject)
                {
                    foreach (var library in pkgConfigProject.Libraries)
                        file.WriteLine(library);
                }
                else
                {
                    file.WriteLine(projectDependency.Name);
                }
            }

            file.WriteLine(")");
        }

        //setup pic
        if (project.UsePIC)
        {
            file.WriteLine("set_property(TARGET " + project.Name + " PROPERTY POSITION_INDEPENDENT_CODE ON)");
        }
    }
}