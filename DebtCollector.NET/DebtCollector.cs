namespace DebtCollector.NET;

public abstract class DebtCollector {

    public static void GenerateDebtReports( 
        string repoPath,
        int groupingDepth
    ) {
        Dictionary<string,int> mostCommittedResult = HotspotFinder.GetMostCommittedFiles(
            repoPath
        );

        const string mostCommittedFilesFileName = @"mostCommittedFiles.csv";
        using StreamWriter mostCommittedFilesWriter = new(mostCommittedFilesFileName);

        mostCommittedFilesWriter.WriteLine( $"Key, Value" );
        foreach (KeyValuePair<string,int> fileCommitCountEntry in mostCommittedResult) {
            mostCommittedFilesWriter.WriteLine( $"{fileCommitCountEntry.Key}, {fileCommitCountEntry.Value}" );
        }

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
        //
        // string mostCommittedFile = mostCommittedResult.First().Key;
        // bool xrayResult = HotspotXray.TryGetXray(
        //     repoPath, 
        //     mostCommittedFile,
        //     out Dictionary<string, long> methodChanges
        // );
        //
        // if( !xrayResult ) {
        //     Console.Error.WriteLine( "Xray failed" );
        //     return;
        // }
        //
        // IEnumerable<KeyValuePair<string, long>> methodChangesFromMostToLeast = methodChanges.OrderBy( m => m.Value ).Reverse();
        // const string xrayFileName = @"xray.csv";
        // using StreamWriter xrayWriter = new(xrayFileName);
        // xrayWriter.WriteLine( $"Key, Value" );
        // foreach (KeyValuePair<string, long> methodChange in methodChangesFromMostToLeast) {
        //     xrayWriter.WriteLine( $"{methodChange.Key}, {methodChange.Value}" );
        // }
    }
}