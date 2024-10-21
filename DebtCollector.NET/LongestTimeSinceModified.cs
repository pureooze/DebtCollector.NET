using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DebtCollector.NET;

public abstract class LongestTimeSinceModified {

    public static IDictionary<string, int> GetFilesWithTimeSinceModified(
        string pathToRepo
    ) {
        return GetFilesWithLastModifiedTime(pathToRepo);
    }
    
    private static IDictionary<string, int> GetFilesWithLastModifiedTime(
        string pathToRepo
    ) {
        Console.WriteLine("Getting files last modified time");

        return GetFilesWithLastModifiedTime_New(pathToRepo);
    }

    private static Dictionary<string, int> GetFilesWithLastModifiedTime_New(
        string pathToRepo
    ) {
        // Step 1: Get the list of tracked files in the repository (those in the current HEAD)
        ImmutableArray<string> trackedFiles = RunGitCommand(pathToRepo, "ls-tree --name-only -r HEAD");

        // Step 2: Loop through each tracked file and get the commit history for that file
        List<FileCommitInfo> files = [];
        DateTime now = DateTime.Now;

        foreach (string file in trackedFiles)
        {
            ImmutableArray<string> commitDate = RunGitCommand(pathToRepo, $"log -1 --pretty=format:\"%at %n\" -- \"{file}\"");
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(commitDate[0])).DateTime;
            files.Add(new FileCommitInfo {
                FileName = file,
                DaysSinceLastCommit = (int)(now - dateTime).TotalDays
            });
        }
        
        return files
            .OrderByDescending(f => f.DaysSinceLastCommit)
            .ToDictionary(fci => fci.FileName, fci => fci.DaysSinceLastCommit);
    }

    // private static Dictionary<string, int> GetFilesWithLastModifiedTime_Old(
    //     string pathToRepo
    // ) {
    //     ProcessStartInfo startInfo = new() {
    //         FileName = "git",
    //         Arguments = "log --pretty=format:\"%at %n\" --name-only",
    //         RedirectStandardOutput = true,
    //         UseShellExecute = false,
    //         CreateNoWindow = true,
    //         WorkingDirectory = pathToRepo
    //     };
    //
    //     Process process = new() {
    //         StartInfo = startInfo
    //     };
    //
    //     process.Start();
    //     List<FileCommitInfo> files = [];
    //
    //     string? commitDate = null;
    //
    //     // Process the output of the Git command
    //     using (var reader = process.StandardOutput)
    //     {
    //         string? line;
    //         while ((line = reader.ReadLine()) != null)
    //         {
    //             if (long.TryParse(line, out long unixTimestamp)) 
    //             {
    //                 // This line is a Unix timestamp, set it as the current commit date
    //                 commitDate = unixTimestamp.ToString();
    //             }
    //             else if (!string.IsNullOrWhiteSpace(line) && commitDate != null)
    //             {
    //                 // This line is a file name, so add it to the list with the commit date
    //                 DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(commitDate)).DateTime;
    //                 files.Add(new FileCommitInfo
    //                 {
    //                     FileName = line,
    //                     DaysSinceLastCommit = dateTime
    //                 });
    //             }
    //         }
    //     }
    //
    //     process.WaitForExit();
    //
    //     DateTime now = DateTime.Now;
    //     // Sort the files by commit date and take the 10 oldest
    //     Dictionary<string, int> oldestFiles = files
    //         .GroupBy(f => f.FileName)
    //         .Select(g => g.First()) // Take the first occurrence (i.e., oldest commit)
    //         .OrderByDescending(f => f.DaysSinceLastCommit)
    //         .ToDictionary(fci => fci.FileName, fci => (int)(now - fci.DaysSinceLastCommit).TotalDays);
    //
    //     return oldestFiles;
    // }
    
    // Method to run a git command and return the output
    static ImmutableArray<string> RunGitCommand( 
        string pathToRepo,
        string arguments
    ) {
        ProcessStartInfo startInfo = new() {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = pathToRepo
        };

        Process process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true        // Enable events
        };
        
        List<string> output = [];
        process.OutputDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine(e.Data);
            output.Add(e.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        return [..output];
    }
}


class FileCommitInfo
{
    public string FileName { get; set; }
    public int DaysSinceLastCommit { get; set; }
}