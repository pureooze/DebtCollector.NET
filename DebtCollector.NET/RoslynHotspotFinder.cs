using DebtCollector.NET.Interfaces;
using LibGit2Sharp;

namespace DebtCollector.NET;

public class RoslynHotspotFinder : IHotspotFinder {
    Dictionary<string,int> IHotspotFinder.GetMostCommittedFiles( 
        string pathToRepo,
        int daysSince
    ) {

        DateTimeOffset since = DateTimeOffset.Now.AddDays(daysSince * -1);

        using Repository repo = new(pathToRepo);
        CommitFilter filter = new() {
            IncludeReachableFrom = repo.Branches.First()
        };

        ICommitLog? commitLog = repo.Commits.QueryBy( filter );
        IEnumerable<Commit> commits = commitLog.Where( c => c.Committer.When > since );

        Dictionary<string, int> fileCommitCounts = new();
            
        foreach (Commit commit in commits) {
           
            // Skip merge commits
            if (commit.Parents.Count() > 1) {
                continue;
            }
            
            foreach (Commit? parent in commit.Parents) {
                TreeChanges? changesSincePreviousCommit = repo.Diff.Compare<TreeChanges>( 
                    oldTree: parent.Tree, 
                    newTree: commit.Tree 
                );
                    
                HashSet<string> storedPaths = [];
                foreach (TreeEntryChanges? change in changesSincePreviousCommit) {
                    if (storedPaths.Contains(change.Path)) {
                        continue;
                    }
                    
                    if ( !fileCommitCounts.TryAdd( change.Path, 1 )) {
                        fileCommitCounts[change.Path]++;
                    } else {
                        storedPaths.Add( change.Path );
                    }
                }
            }
        }
        
        return fileCommitCounts
            .OrderByDescending( x => x.Value )
            .ToDictionary();
    }
}