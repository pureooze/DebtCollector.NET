using System.Diagnostics;
using DebtCollector.NET.Interfaces;

namespace DebtCollector.NET {
    public class GitHotspotFinder : IHotspotFinder {
        IEnumerable<KeyValuePair<string, int>> IHotspotFinder.GetMostCommittedFiles(
            string pathToRepo,
            int daysSince
        ) {
            return GetCommitShas(pathToRepo, daysSince)
                .Where(path => !string.IsNullOrEmpty(path))
                .GroupBy(group => group)
                .Select( group => new KeyValuePair<string, int>( group.Key, group.Count() ) )
                .OrderByDescending( x => x.Value )
                .ToList();;
        }

        private IEnumerable<string> GetCommitShas(
            string pathToRepo,
            int daysSince = 1
        ) {
            List<string> commitShas = [];
            ProcessStartInfo startInfo = new("git") {
                Arguments = $"log --since=\"${daysSince} days ago\" --name-only --pretty=format:",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = pathToRepo
            };

            using Process? process = Process.Start(startInfo);
            while (!process.StandardOutput.EndOfStream) {
                var line = process.StandardOutput.ReadLine();
                commitShas.Add(line);
            }

            return commitShas;
        }
    }
}