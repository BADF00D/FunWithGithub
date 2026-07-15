using FakeItEasy;

namespace GitHubCanvas.Tests;

[TestFixture]
public class GitCommitCreatorTests
{
    private const string RepoPath = @"C:\repo";

    private static IGitProcessRunner CreateRunnerReturning(int versionExitCode, int revParseExitCode, string revParseStdOut)
    {
        var runner = A.Fake<IGitProcessRunner>();

        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._,
                A<string[]>.That.Matches(a => a.SequenceEqual(new[] { "--version" }))))
            .Returns(new GitProcessResult(versionExitCode, "git version 2.44.0", ""));

        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._,
                A<string[]>.That.Matches(a => a.SequenceEqual(new[] { "rev-parse", "--is-inside-work-tree" }))))
            .Returns(new GitProcessResult(revParseExitCode, revParseStdOut, ""));

        return runner;
    }

    [Test]
    public void EnsurePreconditions_Succeeds_WhenGitAvailableAndInsideWorkTree()
    {
        var runner = CreateRunnerReturning(versionExitCode: 0, revParseExitCode: 0, revParseStdOut: "true\n");
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        Assert.DoesNotThrow(() => creator.EnsurePreconditions());
    }

    [Test]
    public void EnsurePreconditions_Throws_WhenGitExecutableMissing()
    {
        var runner = CreateRunnerReturning(versionExitCode: 1, revParseExitCode: 0, revParseStdOut: "true\n");
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        var ex = Assert.Throws<InvalidOperationException>(() => creator.EnsurePreconditions());
        Assert.That(ex!.Message, Does.Contain("git executable not found"));
    }

    [Test]
    public void EnsurePreconditions_Throws_WhenNotInsideWorkTree()
    {
        var runner = CreateRunnerReturning(versionExitCode: 0, revParseExitCode: 0, revParseStdOut: "false\n");
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        var ex = Assert.Throws<InvalidOperationException>(() => creator.EnsurePreconditions());
        Assert.That(ex!.Message, Does.Contain("is not inside a git working tree"));
    }

    [Test]
    public void CreateCommit_PassesAllowEmptyQuietAndMessage_ToRunner()
    {
        var runner = A.Fake<IGitProcessRunner>();
        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._, A<string[]>._))
            .Returns(new GitProcessResult(0, "", ""));
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        creator.CreateCommit(new DateOnly(2026, 1, 4), secondOffset: 0, message: "GitHub canvas art: \"HI\"");

        A.CallTo(() => runner.Run(RepoPath, A<IReadOnlyDictionary<string, string>?>._,
                A<string[]>.That.Matches(a => a.SequenceEqual(
                    new[] { "commit", "--allow-empty", "--quiet", "-m", "GitHub canvas art: \"HI\"" }))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void CreateCommit_SetsAuthorAndCommitterDate_ToNoonUtcPlusSecondOffset()
    {
        var runner = A.Fake<IGitProcessRunner>();
        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._, A<string[]>._))
            .Returns(new GitProcessResult(0, "", ""));
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        creator.CreateCommit(new DateOnly(2026, 1, 4), secondOffset: 5, message: "msg");

        const string expectedDate = "2026-01-04T12:00:05 +00:00";
        A.CallTo(() => runner.Run(A<string>._,
                A<IReadOnlyDictionary<string, string>?>.That.Matches(d =>
                    d != null && d["GIT_AUTHOR_DATE"] == expectedDate && d["GIT_COMMITTER_DATE"] == expectedDate),
                A<string[]>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void CreateCommit_WithoutAuthorOverride_DoesNotSetAuthorEnvVars()
    {
        var runner = A.Fake<IGitProcessRunner>();
        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._, A<string[]>._))
            .Returns(new GitProcessResult(0, "", ""));
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        creator.CreateCommit(new DateOnly(2026, 1, 4), secondOffset: 0, message: "msg");

        A.CallTo(() => runner.Run(A<string>._,
                A<IReadOnlyDictionary<string, string>?>.That.Matches(d =>
                    d != null && !d.ContainsKey("GIT_AUTHOR_NAME") && !d.ContainsKey("GIT_COMMITTER_NAME")),
                A<string[]>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void CreateCommit_WithAuthorOverride_SetsAuthorAndCommitterEnvVars()
    {
        var runner = A.Fake<IGitProcessRunner>();
        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._, A<string[]>._))
            .Returns(new GitProcessResult(0, "", ""));
        var author = new GitAuthor("David Stoermer", "david.stoermer@t2informatik.de");
        var creator = new GitCommitCreator(RepoPath, author, runner);

        creator.CreateCommit(new DateOnly(2026, 1, 4), secondOffset: 0, message: "msg");

        A.CallTo(() => runner.Run(A<string>._,
                A<IReadOnlyDictionary<string, string>?>.That.Matches(d =>
                    d != null &&
                    d["GIT_AUTHOR_NAME"] == "David Stoermer" &&
                    d["GIT_AUTHOR_EMAIL"] == "david.stoermer@t2informatik.de" &&
                    d["GIT_COMMITTER_NAME"] == "David Stoermer" &&
                    d["GIT_COMMITTER_EMAIL"] == "david.stoermer@t2informatik.de"),
                A<string[]>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void CreateCommit_WhenGitFails_ThrowsWithStdErrAndDateInMessage()
    {
        var runner = A.Fake<IGitProcessRunner>();
        A.CallTo(() => runner.Run(A<string>._, A<IReadOnlyDictionary<string, string>?>._, A<string[]>._))
            .Returns(new GitProcessResult(1, "", "fatal: something bad happened"));
        var creator = new GitCommitCreator(RepoPath, author: null, runner);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            creator.CreateCommit(new DateOnly(2026, 1, 4), secondOffset: 0, message: "msg"));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("2026-01-04"));
            Assert.That(ex.Message, Does.Contain("fatal: something bad happened"));
        });
    }
}
