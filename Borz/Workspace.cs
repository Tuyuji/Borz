using Borz.Lua;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using QuickGraph;
using QuickGraph.Algorithms.TopologicalSort;

namespace Borz;


[MoonSharpUserData]
public class Workspace
{
    public string Location = string.Empty;
    public List<Project> Projects = new();

    public string Name;

    public delegate void ProjectAddedEvent(Workspace ws, Project project);

    public event ProjectAddedEvent OnProjectAdded;

    public Workspace(string dir)
    {
        if (!Directory.Exists(dir))
            throw new Exception("Directory given doesn't exist.");
        
        Name = Borz.Config.Get("ws", "name");

        Location = Path.GetFullPath(dir);
    }

    public List<Project>? GetSortedProjectList()
    {
        var graph = new AdjacencyGraph<Project, Edge<Project>>();
        graph.AddVertexRange(Projects);
        foreach (var project in Projects)
        foreach (var dependency in project.Dependencies)
            graph.AddEdge(new Edge<Project>(dependency, project));

        var algorithm = new TopologicalSortAlgorithm<Project, Edge<Project>>(graph);
        try
        {
            algorithm.Compute();
        }
        catch (NonAcyclicGraphException e)
        {
            MugiLog.Fatal("Cyclic/Circular dependency detected, cannot continue.");
            return null;
        }

        return algorithm.SortedVertices.ToList();
    }
    
    public void Compile(Options opt)
    {
        var sortedProjects = GetSortedProjectList();
        if (sortedProjects == null)
        {
            return;
        }
        
        MugiLog.Info($"Compiling workspace \"{Name}\" for target: {opt.GetTarget()}");
        
        sortedProjects.ForEach(prj =>
        {
            var builder = BuildFactory.GetBuilder(prj.Language);
            var result = builder.Build(prj, opt);
            if(!result.success)
                MugiLog.Fatal($"Failed to compile project: {result.error}");
        });
    }

    [MoonSharpHidden]
    public void Add(Project prj)
    {
        if(Projects.Contains(prj))
            return;
        prj.Owner = this;
        Projects.Add(prj);
        OnProjectAdded?.Invoke(this, prj);
    }

    [MoonSharpHidden]
    public void Remove(Project prj)
    {
        if(!Projects.Contains(prj))
            return;

        prj.Owner = null;
        Projects.Remove(prj);
    }

    [MoonSharpVisible(true)]
    private void add(Project prj)
    {
        Add(prj);
    }
    
    [MoonSharpVisible(true)]
    private void remove(Project prj)
    {
        Remove(prj);
    }

    [MoonSharpVisible(true)]
    private Project new_project(Script script, string name, string lang, BinType type, string[]? tags = null)
    {
        var proj = Project.Create(lang, name, type, script.GetCwd());
        if(tags != null)
            proj.Tags.AddRange(tags);
        Add(proj);
        return proj;
    }
}