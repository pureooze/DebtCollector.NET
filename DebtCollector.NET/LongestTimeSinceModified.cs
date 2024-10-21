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
            EnableRaisingEvents = true
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