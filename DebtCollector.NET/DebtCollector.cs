using DebtCollector.NET.Interfaces;

namespace DebtCollector.NET;

public class DebtCollector(
    IHotspotFinder hotspotFinder
) {
    public void GenerateDebtReports(
        string repoPath,
        string[] modes,
        int groupingDepth,
        int daysSince = 1
    ) {
        
        Dictionary<string,int> mostCommittedResult = GenerateFileHotspotReport(
            repoPath, 
            daysSince
        );

        if (modes.Contains("hotspotGrouped")) {
            GenerateGroupedHotspotPathsReport(
                groupingDepth,
                mostCommittedResult
            );
        }
        
        if (modes.Contains("MethodXray")) {
            GenerateMethodXrayReport(
                repoPath, 
                mostCommittedResult, 
                daysSince
            );
        }

        if (modes.Contains("Complexity")) {
            GenerateComplexityReport(
                repoPath, 
                mostCommittedResult
            );
        }

        GenerateFilesWithTimeSinceModifiedReport(repoPath);
        
        Console.WriteLine("Debt reports generated");
    }
    
    private void GenerateFilesWithTimeSinceModifiedReport(
        string repoPath
    ) {
        IDictionary<string, int> filesWithTimeSinceModified = 
            LongestTimeSinceModified.GetFilesWithTimeSinceModified(repoPath);
        
        const string pathGroupingFilesFileName = @"filesWithTimeSinceModified.csv";
        using StreamWriter pathGroupingFilesWriter = new(pathGroupingFilesFileName);

        pathGroupingFilesWriter.WriteLine( $"Key, Value" );
        foreach (KeyValuePair<string,int> grouping in filesWithTimeSinceModified) {
            pathGroupingFilesWriter.WriteLine( $"{grouping.Key}, {grouping.Value}" );
        }
    }

    private void GenerateGroupedHotspotPathsReport(
        int groupingDepth,
        Dictionary<string, int> mostCommittedResult
    ) {
        Dictionary<string, int> groupChangeCount = PathGrouper.GetGroupedPaths(
            depth: groupingDepth, 
            mostCommittedResult: mostCommittedResult
        );
        
        const string pathGroupingFilesFileName = @"pathGrouping.csv";
        using StreamWriter pathGroupingFilesWriter = new(pathGroupingFilesFileName);

        pathGroupingFilesWriter.WriteLine( $"Key, Value" );
        foreach (KeyValuePair<string,int> grouping in groupChangeCount) {
            pathGroupingFilesWriter.WriteLine( $"{grouping.Key}, {grouping.Value}" );
        }
    }

    private void GenerateComplexityReport(
        string repoPath, 
        Dictionary<string, int> mostCommittedResult 
    ) {
        Console.WriteLine("Getting complexity");
        IEnumerable<KeyValuePair<string, long>> methodComplexityFromAllHotspotFiles = [];
        foreach (KeyValuePair<string, int> keyValuePair in mostCommittedResult.Take(50)) {
            bool complexityResult = ComplexityCalculator.TryCalculateComplexity(
                repoPath,
                keyValuePair.Key,
                out Dictionary<string, long> methodComplexity
            );

            if (!complexityResult) {
                Console.Error.WriteLine($"Complexity failed for: {repoPath} : {keyValuePair.Key}");
            }

            methodComplexityFromAllHotspotFiles = [
                ..methodComplexityFromAllHotspotFiles,
                ..methodComplexity
            ];
        }

        string complexityFileName = @"complexity.csv";
        using StreamWriter complexityWriter = new(complexityFileName);
        complexityWriter.WriteLine($"Key;Complexity");
        IEnumerable<KeyValuePair<string, long>> orderedMethodComplexityFromAllHotspotFiles =
            methodComplexityFromAllHotspotFiles.OrderBy(m => m.Value).Reverse();
        foreach (KeyValuePair<string, long> methodComplexity in orderedMethodComplexityFromAllHotspotFiles) {
            complexityWriter.WriteLine($"{methodComplexity.Key}; {methodComplexity.Value}");
        }
    }

    private void GenerateMethodXrayReport(
        string repoPath,
        Dictionary<string, int> mostCommittedResult,
        int daysSince = 1
    ) {
        Console.WriteLine("Getting xray hotspots");
        IEnumerable<KeyValuePair<string, int>> csFiles = mostCommittedResult.Where(n => n.Key.EndsWith(".cs")).Take(3);

        IEnumerable<KeyValuePair<string, long>> methodChangesFromAllHotspotFiles = [];
        foreach (KeyValuePair<string,int> file in csFiles) {
            bool xrayResult = HotspotXray.TryGetXray(
                repoPath,
                filePath: file.Key,
                out Dictionary<string, long> methodChanges,
                daysSince: daysSince
            );

            if (!xrayResult) {
                Console.Error.WriteLine("Xray failed");
                return;
            }

            methodChangesFromAllHotspotFiles = [
                ..methodChangesFromAllHotspotFiles,
                ..methodChanges
            ];
        }

        string xrayFileName = @"xray.csv";
        using StreamWriter xrayWriter = new(xrayFileName);

        xrayWriter.WriteLine($"Key;Value");
        IEnumerable<KeyValuePair<string, long>> orderedMethodChangesFromAllHotspotFiles = methodChangesFromAllHotspotFiles.OrderBy(m => m.Value).Reverse();
        foreach (KeyValuePair<string, long> methodChange in orderedMethodChangesFromAllHotspotFiles) {
            xrayWriter.WriteLine($"{methodChange.Key};{methodChange.Value}");
        }
    }

    private Dictionary<string, int> GenerateFileHotspotReport(
        string repoPath,
        int daysSince = 1
    ) {
        Console.WriteLine("Getting hotspots");
        Dictionary<string, int> mostCommittedResult = hotspotFinder.GetMostCommittedFiles(
            repoPath,
            daysSince: daysSince
        );

        string mostCommittedFilesFileName = @"mostCommittedFiles.csv";
        using StreamWriter mostCommittedFilesWriter = new(mostCommittedFilesFileName);

        mostCommittedFilesWriter.WriteLine($"Key;Value");
        foreach (KeyValuePair<string, int> fileCommitCountEntry in mostCommittedResult) {
            mostCommittedFilesWriter.WriteLine($"{fileCommitCountEntry.Key};{fileCommitCountEntry.Value}");
        }

        return mostCommittedResult;
    }
}