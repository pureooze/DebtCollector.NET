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
        out Dictionary<string, long> methodChangeCount,
        int daysSince = 1
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
            daysSince: daysSince,
            commits: out List<Commit> commits
        );

        if( !gotCommits ) {
            return false;
        }

        for( int i = 0; i < commits.Count - 1; i++ ) {

            TreeEntry? oldContent = commits[i][filePath];
            TreeEntry? newContent = commits[i + 1][filePath];

            if (oldContent == null || newContent == null) {
                continue;
            }
            
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
                filePath,
                oldContentTree,
                newContentTree
            );
            
            foreach (string method in rawChanges) {
                if (methodChangeCount.ContainsKey($"{filePath}.{method}")) {
                    methodChangeCount[$"{filePath}.{method}"] += 1;
                } else {
                    methodChangeCount[$"{filePath}.{method}"] = 1;
                }
            }
        }

        return true;
    }

    private static bool TryGetCommitShas(
        Repository repo,
        string filePath,
        out List<Commit> commits,
        int daysSince = 1
    ) {
        commits = [];

        DateTimeOffset since = DateTimeOffset.Now.AddDays( daysSince * -1 );

        CommitFilter filter = new() {
            IncludeReachableFrom = repo.Branches.First(),
        };

        int i = 0;
        foreach (Commit? commit in repo.Commits.QueryBy( filter )) {

            i++;
            TreeChanges? changes = repo.Diff.Compare<TreeChanges>(
                commit.Parents.FirstOrDefault()?.Tree,
                commit.Tree
            );

            TreeEntryChanges? change = changes.FirstOrDefault(
                c => c.Path.Equals( filePath, StringComparison.OrdinalIgnoreCase )
            );

            if (commit.Author.When < since) {
                break;
            }
            
            if( change == null ) {
                continue;
            }

            commits.Add( commit );
        }

        return true;
    }

    private static IEnumerable<string> GetDetailedChanges(
        string filePath,
        SyntaxTree oldTree,
        SyntaxTree newTree
    ) {
        SyntaxNode oldRoot = oldTree.GetRoot();
        SyntaxNode newRoot = newTree.GetRoot();

        var oldMethods = oldRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary( m => filePath + GetNodeIdentifier(m), m => m.ToString() );
        var newMethods = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary( m => filePath + GetNodeIdentifier(m), m => m.ToString() );

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

    private static string GetNodeIdentifier(MethodDeclarationSyntax method) {
        // return GetParentClassName(m) + "." + m.Identifier.ValueText + "." + m.ParameterList;
        var namespaceDecl = method.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        var namespaceName = namespaceDecl != null ? namespaceDecl.Name.ToString() : "<Global>";
        var classHierarchy = method.Ancestors().OfType<ClassDeclarationSyntax>()
            .Select(c => c.Identifier.Text)
            .Reverse() // To start from the outermost class
            .ToList();
        var fullClassName = string.Join(".", classHierarchy);
        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => p.Type.ToString()));
        var filePath = method.SyntaxTree.FilePath.Replace("\\", "/");
        var methodBody = method.Body?.ToString() ?? method.ExpressionBody?.ToString();
        var hash = string.IsNullOrEmpty(methodBody) ? "" : Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(methodBody)));

        return $"{filePath}:{namespaceName}.{fullClassName}.{method.Identifier.Text}({parameters})-{hash}";
    }

    public static string GetParentClassName(MethodDeclarationSyntax methodSyntax) {
        // Traverse up the syntax tree to find the ClassDeclarationSyntax
        var classDeclaration = methodSyntax.Parent;
        while (classDeclaration != null && !(classDeclaration is ClassDeclarationSyntax))
        {
            classDeclaration = classDeclaration.Parent;
        }

        // Check if a ClassDeclarationSyntax was found
        if (classDeclaration is ClassDeclarationSyntax classSyntax)
        {
            // Return the name of the class
            return classSyntax.Identifier.Text;
        }

        // Return null or throw an exception if no parent class is found
        return null;
    }
}