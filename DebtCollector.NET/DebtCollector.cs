namespace DebtCollector.NET;

public abstract class DebtCollector {

    public static void GenerateDebtReports( 
        string repoPath
    ) {
        IList<KeyValuePair<string,int>> mostCommittedResult = HotspotFinder.GetMostCommittedFiles(
            repoPath
        ).ToList();

        string mostCommittedFilesFileName = @"mostCommittedFiles.csv";
        using StreamWriter mostCommittedFilesWriter = new(mostCommittedFilesFileName);

        mostCommittedFilesWriter.WriteLine( $"Key, Value" );
        foreach (KeyValuePair<string,int> fileCommitCountEntry in mostCommittedResult) {
            mostCommittedFilesWriter.WriteLine( $"{fileCommitCountEntry.Key}, {fileCommitCountEntry.Value}" );
        }

        string mostCommittedFile = mostCommittedResult.First().Key;
        bool xrayResult = HotspotXray.TryGetXray(
            repoPath, 
            mostCommittedFile,
            out Dictionary<string, long> methodChanges
        );

        if( !xrayResult ) {
            Console.Error.WriteLine( "Xray failed" );
            return;
        }

        IEnumerable<KeyValuePair<string, long>> methodChangesFromMostToLeast = methodChanges.OrderBy( m => m.Value ).Reverse();
        string xrayFileName = @"xray.csv";
        using StreamWriter xrayWriter = new(xrayFileName);
        xrayWriter.WriteLine( $"Key, Value" );
        foreach (KeyValuePair<string, long> methodChange in methodChangesFromMostToLeast) {
            xrayWriter.WriteLine( $"{methodChange.Key}, {methodChange.Value}" );
        }
    }
}