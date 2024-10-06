using LibGit2Sharp;

namespace DebtCollector.NET;

public abstract class HotspotFinder {

    public static Dictionary<string,int> GetMostCommittedFiles( 
        string pathToRepo 
    ) {

        DateTimeOffset since = DateTimeOffset.Now.AddYears(-1);

        using Repository repo = new(pathToRepo);
        CommitFilter filter = new() {
            IncludeReachableFrom = repo.Branches
        };

        ICommitLog? commitLog = repo.Commits.QueryBy( filter );
        IEnumerable<Commit> commits = commitLog.Where( c => c.Committer.When > since );

        Dictionary<string, int> fileCommitCounts = new();
            
        foreach (Commit commit in commits) {
            foreach (Commit? parent in commit.Parents) {
                TreeChanges? changesSincePreviousCommit = repo.Diff.Compare<TreeChanges>( 
                    oldTree: parent.Tree, 
                    newTree: commit.Tree 
                );
                    
                foreach (TreeEntryChanges? change in changesSincePreviousCommit) {
                    if (!fileCommitCounts.TryAdd( change.Path, 1 )) {
                        fileCommitCounts[change.Path]++;
                    }
                }
            }
        }
        
        return fileCommitCounts.OrderByDescending( x => x.Value ).ToDictionary();
    }
}