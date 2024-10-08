// See https://aka.ms/new-console-template for more information

using DebtCollector.NET;
using DebtCollector.NET.Interfaces;
using Microsoft.Extensions.Configuration;

Console.WriteLine(value: "Hello, World! from DebtCollector.NET");

IConfigurationBuilder builder = new ConfigurationBuilder()
    .SetBasePath(basePath: Directory.GetCurrentDirectory())
    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true);

IConfigurationRoot config = builder.Build();
string? repoPath = config[key: "PathToRepo"];
int daysSince = Int32.Parse(config[key: "DaysSince"] ?? string.Empty);
string[] modes = config.GetSection("Modes").Get<string[]>() ?? [];
string gitClient = config[key: "GitClient"] ?? "libgit2sharp";

if (string.IsNullOrEmpty(value: repoPath)) {
    Console.WriteLine(value: "No repo path found in appsettings.json");
    return;
}

if (modes.Length == 0) {
    Console.WriteLine(value: "No modes found in appsettings.json");
    return;
}

IHotspotFinder hotspotFinder;
switch (gitClient) {
    case "cli":
        Console.WriteLine(value: "Using CLI git client");
        hotspotFinder = new GitHotspotFinder();
        break;
    case "libgit2sharp":
    default:
        Console.WriteLine(value: "Using LibGit2Sharp git client");
        hotspotFinder = new RoslynHotspotFinder();
        break;
        
}

DebtCollector.NET.DebtCollector debtCollector = new(
    hotspotFinder: hotspotFinder
);

debtCollector.GenerateDebtReports(
    repoPath: repoPath, 
    modes: modes,
    daysSince: daysSince
);