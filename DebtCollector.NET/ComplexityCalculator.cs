using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DebtCollector.NET {
    public class ComplexityCalculator {
        public static bool TryCalculateComplexity(
            string repoPath,
            string filePath,
            out Dictionary<string, long> methodComplexity
        ) {
            methodComplexity = [];

            Repository repo = new(repoPath);
            Commands.Checkout(repo, "master");

            // Full path to the file
            string fullFilePath = $"{repoPath}\\{filePath}";

            // Read the file content
            if (!File.Exists(fullFilePath)) {
                return false;
            }
            string fileContent = File.ReadAllText(fullFilePath);

            // Parse the file content to create a Syntax Tree
            SyntaxTree tree = CSharpSyntaxTree.ParseText(fileContent);

            // Get the root node of the tree (contains the entire file)
            SyntaxNode root = tree.GetRoot();

            // Find all method declarations in the file
            var methodDeclarations = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methodDeclarations) {
                // Calculate the cyclomatic complexity of the method
                int complexity = CalculateCyclomaticComplexity(method.ToString());
                string methodName = method.Identifier.Text;
                string parameters = method.ParameterList.Parameters.Any() 
                    ? method.ParameterList.Parameters
                        .Select(p => $"{p.Type} {p.Identifier}")
                        .Aggregate((current, next) => $"{current}, {next}")
                    : "";

                string uniqueMethodName = $"{methodName}({parameters})";
                
                methodComplexity.TryAdd($"{filePath}.{uniqueMethodName}", complexity);
            }

            return true;
        }

        private static int CalculateCyclomaticComplexity(
            string sourceCode
        ) {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
            CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

            int complexity = 1; // Starting at 1 for the method itself
            SyntaxWalker walker = new();
            walker.Visit(root);

            return complexity + walker.DecisionPoints;
        }

        class SyntaxWalker : CSharpSyntaxWalker {
            public int DecisionPoints { get; private set; } = 0;

            public override void VisitIfStatement(
                IfStatementSyntax node
            ) {
                DecisionPoints++;
                base.VisitIfStatement(node);
            }

            public override void VisitWhileStatement(
                WhileStatementSyntax node
            ) {
                DecisionPoints++;
                base.VisitWhileStatement(node);
            }

            public override void VisitForStatement(
                ForStatementSyntax node
            ) {
                DecisionPoints++;
                base.VisitForStatement(node);
            }

            public override void VisitForEachStatement(
                ForEachStatementSyntax node
            ) {
                DecisionPoints++;
                base.VisitForEachStatement(node);
            }

            public override void VisitDoStatement(
                DoStatementSyntax node
            ) {
                DecisionPoints++;
                base.VisitDoStatement(node);
            }

            public override void VisitSwitchStatement(
                SwitchStatementSyntax node
            ) {
                // Each case is considered a separate path, hence adds to complexity.
                // Note: We assume the default case exists implicitly, which adds to complexity as well.
                DecisionPoints += node.Sections.Count;
                base.VisitSwitchStatement(node);
            }

            public override void VisitConditionalExpression(
                ConditionalExpressionSyntax node
            ) {
                // Ternary operator (?:) also contributes to the complexity.
                DecisionPoints++;
                base.VisitConditionalExpression(node);
            }


            // Add more visit methods for other syntax types that represent decision points.
        }
    }
}