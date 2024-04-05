// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;

Console.WriteLine( "Hello, World! from TechDebtDiscovery" );

IConfigurationBuilder builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfigurationRoot config = builder.Build();
string? repoPath = config["PathToRepo"];

if( string.IsNullOrEmpty( repoPath ) ) {
    Console.WriteLine( "No repo path found in appsettings.json" );
    return;
}

DebtCollector.NET.DebtCollector.GenerateDebtReports( repoPath );