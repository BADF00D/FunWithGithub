using System.Diagnostics;
using System.Globalization;

namespace GitHubCanvas;

internal readonly record struct GitProcessResult(int ExitCode, string StdOut, string StdErr);

/// <summary>Abstraction over invoking the git executable, so it can be faked in tests.</summary>
internal interface IGitProcessRunner
{
    GitProcessResult Run(string workingDirectory, IReadOnlyDictionary<string, string>? envOverrides, params string[] arguments);
}

/// <summary>Invokes the real git executable via <see cref="Process"/>.</summary>
internal sealed class ProcessGitRunner : IGitProcessRunner
{
    public GitProcessResult Run(string workingDirectory, IReadOnlyDictionary<string, string>? envOverrides, params string[] arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        if (envOverrides is not null)
        {
            foreach (var (key, value) in envOverrides)
            {
                psi.Environment[key] = value;
            }
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git process.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new GitProcessResult(process.ExitCode, stdout, stderr);
    }
}

/// <summary>Creates backdated, empty git commits to "paint" pixels onto the contribution graph.</summary>
internal sealed class GitCommitCreator
{
    private readonly string _repositoryPath;
    private readonly GitAuthor? _author;
    private readonly IGitProcessRunner _runner;

    public GitCommitCreator(string repositoryPath, GitAuthor? author)
        : this(repositoryPath, author, new ProcessGitRunner())
    {
    }

    internal GitCommitCreator(string repositoryPath, GitAuthor? author, IGitProcessRunner runner)
    {
        _repositoryPath = repositoryPath;
        _author = author;
        _runner = runner;
    }

    /// <summary>Fails fast with a clear message if git isn't installed or the path isn't a git working tree.</summary>
    public void EnsurePreconditions()
    {
        var versionResult = _runner.Run(_repositoryPath, envOverrides: null, "--version");
        if (versionResult.ExitCode != 0)
        {
            throw new InvalidOperationException("git executable not found. Make sure git is installed and on PATH.");
        }

        var workTreeResult = _runner.Run(_repositoryPath, envOverrides: null, "rev-parse", "--is-inside-work-tree");
        if (workTreeResult.ExitCode != 0 || workTreeResult.StdOut.Trim() != "true")
        {
            throw new InvalidOperationException($"'{_repositoryPath}' is not inside a git working tree.");
        }
    }

    /// <summary>Creates one empty commit backdated to <paramref name="date"/> at noon UTC plus <paramref name="secondOffset"/> seconds.</summary>
    public void CreateCommit(DateOnly date, int secondOffset, string message)
    {
        var timestamp = date.ToDateTime(new TimeOnly(12, 0, 0)).AddSeconds(secondOffset);
        var isoDate = timestamp.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + " +00:00";

        var envOverrides = new Dictionary<string, string>
        {
            ["GIT_AUTHOR_DATE"] = isoDate,
            ["GIT_COMMITTER_DATE"] = isoDate,
        };

        if (_author is not null)
        {
            envOverrides["GIT_AUTHOR_NAME"] = _author.Name;
            envOverrides["GIT_AUTHOR_EMAIL"] = _author.Email;
            envOverrides["GIT_COMMITTER_NAME"] = _author.Name;
            envOverrides["GIT_COMMITTER_EMAIL"] = _author.Email;
        }

        var result = _runner.Run(_repositoryPath, envOverrides, "commit", "--allow-empty", "--quiet", "-m", message);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"git commit failed for {date:yyyy-MM-dd}: {result.StdErr.Trim()}");
        }
    }
}
