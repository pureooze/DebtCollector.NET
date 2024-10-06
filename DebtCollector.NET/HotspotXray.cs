using System.Text;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DebtCollector.NET;

public abstract class HotspotXray {

    public static bool TryGetXray(
        string pathToRepo,
        string filePath,
        out Dictionary<string, long> methodChangeCount
    ) {
        methodChangeCount = [];
        if( string.IsNullOrEmpty( pathToRepo ) ) {
            return false;
        }

        if( string.IsNullOrEmpty( filePath ) ) {
            return false;
        }

        using Repository repo = new( pathToRepo );
        bool gotCommits = TryGetCommitShas(
            repo: repo,
            filePath: filePath,
            commits: out List<Commit> commits
        );

        if( !gotCommits ) {
            return false;
        }

        for( int i = 0; i < commits.Count - 1; i++ ) {

            TreeEntry? oldContent = commits[i][filePath];
            TreeEntry? newContent = commits[i + 1][filePath];

            Blob? oldContentBlob = oldContent.Target as Blob;
            Blob? newContentBlob = newContent.Target as Blob;

            using StreamReader oldContentStream = new(
                oldContentBlob.GetContentStream(),
                Encoding.UTF8
            );

            using StreamReader newContentStream = new(
                newContentBlob.GetContentStream(),
                Encoding.UTF8
            );

            SyntaxTree oldContentTree = CSharpSyntaxTree.ParseText( oldContentStream.ReadToEnd() );
            SyntaxTree newContentTree = CSharpSyntaxTree.ParseText( newContentStream.ReadToEnd() );

            IEnumerable<string> rawChanges = GetDetailedChanges(
                oldContentTree,
                newContentTree
            );
            
            foreach (string method in rawChanges) {
                if (methodChangeCount.ContainsKey(method)) {
                    methodChangeCount[method]++;
                } else {
                    methodChangeCount[method] = 1;
                }
            }
        }

        return true;
    }

    private static bool TryGetCommitShas(
        Repository repo,
        string filePath,
        out List<Commit> commits
    ) {
        commits = [];

        DateTimeOffset since = DateTimeOffset.Now.AddYears( -1 );

        CommitFilter filter = new() {
            IncludeReachableFrom = repo.Branches["master"],
        };

        foreach (Commit? commit in repo.Commits.QueryBy( filter )) {

            TreeChanges? changes = repo.Diff.Compare<TreeChanges>(
                commit.Parents.FirstOrDefault()?.Tree,
                commit.Tree
            );

            TreeEntryChanges? change = changes.FirstOrDefault(
                c => c.Path.Equals( filePath, StringComparison.OrdinalIgnoreCase )
            );

            if( change == null || commit.Author.When < since ) {
                continue;
            }

            commits.Add( commit );
        }

        return true;
    }

    private static IEnumerable<string> GetDetailedChanges(
        SyntaxTree oldTree,
        SyntaxTree newTree
    ) {
        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        var oldMethods = oldRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary( m => m.Identifier.ValueText + m.ParameterList, m => m.ToString() );
        var newMethods = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary( m => m.Identifier.ValueText + m.ParameterList, m => m.ToString() );

        var allMethodKeys = oldMethods.Keys.Union( newMethods.Keys ).Distinct();

        var changedMethods = new List<string>();

        foreach (var key in allMethodKeys) {
            oldMethods.TryGetValue( key, out var oldMethodBody );
            newMethods.TryGetValue( key, out var newMethodBody );

            if( !string.Equals( oldMethodBody, newMethodBody ) ) {
                changedMethods.Add( key.Split( '(' ).First() ); // Extract method name without parameters for reporting
            }
        }

        return changedMethods.Distinct();
    }
}