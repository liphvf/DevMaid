using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

var solutionPath = args.Length > 0 ? args[0] : "..\\DevMaid.slnx";
var outputPath = args.Length > 1 ? args[1] : "..\\analysis-report.md";

MSBuildLocator.RegisterDefaults();

using var workspace = MSBuildWorkspace.Create();

var solutionFullPath = Path.GetFullPath(solutionPath);
Console.WriteLine($"Opening solution: {solutionFullPath}");
var solution = await workspace.OpenSolutionAsync(Path.GetFullPath(solutionPath));

var md = new List<string>
{
    "# Code Analysis Report\n"
};

int diagnosticCount = 0;

foreach (var project in solution.Projects)
{
    var compilation = await project.GetCompilationAsync();
    if (compilation is null) continue;

    var diagnostics = compilation.GetDiagnostics();

    foreach (var diag in diagnostics)
    {
        var location = diag.Location.GetLineSpan();

        md.Add($"## [{diag.Severity}] {diag.Id} — {project.Name}");
        md.Add($"- {diag.GetMessage()}");
        md.Add($"- Arquivo: {location.Path}");
        md.Add($"- Linha: {location.StartLinePosition.Line + 1}");
        md.Add("");
        diagnosticCount++;
    }
}

if (diagnosticCount == 0)
    md.Add("_Nenhum diagnóstico encontrado._");

var outputFullPath = Path.GetFullPath(outputPath);
File.WriteAllLines(outputFullPath, md);
Console.WriteLine($"Report written to: {outputFullPath} ({diagnosticCount} diagnostic(s))");
