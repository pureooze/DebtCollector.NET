using System.Runtime.InteropServices;

namespace DebtCollector.NET;

public abstract class PathGrouper {

    public static Dictionary<string, int> GetGroupedPaths(
        int depth,
        Dictionary<string,int> mostCommittedResult
    ) {
        if (depth < 0) {
            return [];
        }

        if( depth == 0 ) {
            return mostCommittedResult;
        }
        
        string separator = GetSeparator();
        
        Dictionary<string, int> groupChangeCount = new();
        foreach (KeyValuePair<string,int> fileCommitCountEntry in mostCommittedResult) {
            string path = fileCommitCountEntry.Key;
            string[] pathParts = path.Split( separator );
            string groupedPath = string.Join( separator, pathParts.Take( depth ) );
            if (groupChangeCount.ContainsKey( groupedPath )) {
                groupChangeCount[groupedPath] += fileCommitCountEntry.Value;
            } else {
                groupChangeCount[groupedPath] = fileCommitCountEntry.Value;
            }
        }
        
        return groupChangeCount.OrderByDescending( x => x.Value ).ToDictionary();
    }

    private static string GetSeparator() {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\" : "/";
    }
}