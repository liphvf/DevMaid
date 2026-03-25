MSBuildLocator.RegisterDefaults();

using var workspace = MSBuildWorkspace.Create();

var solution = await workspace.OpenSolutionAsync("..\\DevMaid.slnx");

var md = new List<string>
{
    "# Code Analysis Report\n"
};

foreach (var project in solution.Projects)
{
    var compilation = await project.GetCompilationAsync();

    var diagnostics = compilation.GetDiagnostics();

    foreach (var diag in diagnostics)
    {
        var location = diag.Location.GetLineSpan();

        md.Add($"## {diag.Id}");
        md.Add($"- {diag.GetMessage()}");
        md.Add($"- Arquivo: {location.Path}");
        md.Add($"- Linha: {location.StartLinePosition.Line + 1}");
        md.Add("");
    }
}

File.WriteAllLines("report.md", md);
