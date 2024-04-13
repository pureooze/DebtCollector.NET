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

if (string.IsNullOrEmpty(value: repoPath)) {
    Console.WriteLine(value: "No repo path found in appsettings.json");
    return;
}

IHotspotFinder hotspotFinder = new GitHotspotFinder();
DebtCollector.NET.DebtCollector debtCollector = new(
    hotspotFinder: hotspotFinder
);

debtCollector.GenerateDebtReports(repoPath: repoPath, daysSince: daysSince);