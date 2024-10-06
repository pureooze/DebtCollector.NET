# DebtCollector.NET

This is a simple Modern .NET tool that lets you perform analysis on your code to quickly discover potential technical debt hotspots.
The goal is to help prioritize work, so instead of trying to focus on `1000000` lines of code for refactors we can narrow down our focus to `500` lines (these numbers are just an example, the point is we want to reduce the focus).

The tool leverages `LibGit2Sharp` and `Roslyn` to perform analysis on C# code in a git repo and was inspired by Adam Tornhills talk [Prioritizing Technical Debt as If Time & Money Matters](https://www.youtube.com/watch?v=w9YhmMPLQ4U).

## How To Use It
Create an `appsettings.json` file in the `DebtCollector.NET` directory with a `PathToRepo` property, for example:
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Debug",
            "System": "Information",
            "Microsoft": "Information"
        }
    },
    "PathToRepo": "C:\\My\\Code\\Projects\\TwitchEverywhere",
    "GroupDepth": 1
}
```


Run the `DebtCollector.NET` project and it will output two CSV files:
* `mostCommittedFiles.csv`: A list of files sorted by most committed
* `xray.csv`: A list of all methods from the first file in the previous list sorted by most committed

For example running this on the [`TwitchEverywhere`](https://github.com/pureooze/TwitchEverywhere) code we get an output like this:

![TwitchEverywhere-commit-count-per-file-cs.webp](DebtCollector.NET/assets/TwitchEverywhere-commit-count-per-file-cs.webp)

Notice the outlier? It has almost double the commits as each of the other files!
To get more details on why that might be, we can look at the Xray results and see this:

![TwitchEverywhere-commit-count-per-file-cs.webp](DebtCollector.NET/assets/TwitchEverywhere-commit-count-per-method.webp)

Now its clear that the `MessageCallback` method is something that developers work with extremely often!
So we can focus on that when looking to do refactors. 🎉

## Path Grouping
`DebtCollector.NET` can also group paths to a certain depth through the `GroupDepth` setting.
For example if we have the following paths:
```json
[
    { "path": "first/first/path/to/file1", "count": 10 },
    { "path": "first/first/path/to/file2", "count": 2 },
    { "path": "first/second/path/to/file3", "count": 50 },
    { "path": "first/second/path/to/file4", "count": 4 },
    { "path": "first/third/path/to/file5", "count": 9 }
]
```

And we set `GroupDepth` to `1`, we will get the following output:
```json
[
  { "path": "first", "count": 75 }
]
```

If no `GroupDepth` is set, the output will be the same as the full paths list.