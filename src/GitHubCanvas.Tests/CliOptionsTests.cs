namespace GitHubCanvas.Tests;

[TestFixture]
public class CliOptionsTests
{
    [Test]
    public void Parse_MinimalValidArgs_UsesDefaults()
    {
        var options = CliOptions.Parse(["--text", "HI", "--start-date", "2026-01-04"]);

        Assert.Multiple(() =>
        {
            Assert.That(options.Text, Is.EqualTo("HI"));
            Assert.That(options.StartDate, Is.EqualTo(new DateOnly(2026, 1, 4)));
            Assert.That(options.CommitsPerDate, Is.EqualTo(1));
            Assert.That(options.Author, Is.Null);
        });
    }

    [Test]
    public void Parse_AllArgsProvided_ParsesEverything()
    {
        var options = CliOptions.Parse([
            "--text", "OK",
            "--start-date", "2026-01-04",
            "--commits-per-date", "3",
            "--author", "David Stoermer <david.stoermer@t2informatik.de>",
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(options.Text, Is.EqualTo("OK"));
            Assert.That(options.CommitsPerDate, Is.EqualTo(3));
            Assert.That(options.Author, Is.Not.Null);
            Assert.That(options.Author!.Name, Is.EqualTo("David Stoermer"));
            Assert.That(options.Author!.Email, Is.EqualTo("david.stoermer@t2informatik.de"));
        });
    }

    [Test]
    public void Parse_MissingText_Throws()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--start-date", "2026-01-04"]));

        Assert.That(ex!.Message, Does.Contain("--text is required"));
        Assert.That(ex.IsHelpRequest, Is.False);
    }

    [Test]
    public void Parse_MissingStartDate_Throws()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI"]));

        Assert.That(ex!.Message, Does.Contain("--start-date is required"));
    }

    [Test]
    public void Parse_UnsupportedCharacters_ReportsAllOfThemAtOnce()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI!ÄÖ", "--start-date", "2026-01-04"]));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("'!'"));
            Assert.That(ex.Message, Does.Contain("'Ä'"));
            Assert.That(ex.Message, Does.Contain("'Ö'"));
        });
    }

    [Test]
    public void Parse_TextWiderThanAYear_IsAccepted()
    {
        // The canvas is not capped at 52 columns: wider text just produces a wider grid,
        // spanning as many weeks as needed.
        var options = CliOptions.Parse(["--text", "ABCDEFGHI ABCDEFGHI", "--start-date", "2026-01-04"]);

        Assert.That(options.Text, Is.EqualTo("ABCDEFGHI ABCDEFGHI"));
    }

    [Test]
    public void Parse_InvalidDateFormat_Throws()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI", "--start-date", "04.01.2026"]));

        Assert.That(ex!.Message, Does.Contain("not a valid date"));
    }

    [Test]
    public void Parse_StartDateNotASunday_ThrowsWithNearestSundaysSuggested()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI", "--start-date", "2026-01-03"]));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("must be a Sunday"));
            Assert.That(ex.Message, Does.Contain("2025-12-28"));
            Assert.That(ex.Message, Does.Contain("2026-01-04"));
        });
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("abc")]
    public void Parse_InvalidCommitsPerDate_Throws(string value)
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI", "--start-date", "2026-01-04", "--commits-per-date", value]));

        Assert.That(ex!.Message, Does.Contain("must be a positive integer"));
    }

    [TestCase("david.stoermer@t2informatik.de")]
    [TestCase("David Stoermer david.stoermer@t2informatik.de")]
    [TestCase("David Stoermer <david.stoermer@t2informatik.de")]
    public void Parse_InvalidAuthorFormat_Throws(string value)
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI", "--start-date", "2026-01-04", "--author", value]));

        Assert.That(ex!.Message, Does.Contain("must be in the form"));
    }

    [Test]
    public void Parse_UnknownArgument_Throws()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text", "HI", "--start-date", "2026-01-04", "--push"]));

        Assert.That(ex!.Message, Does.Contain("Unknown argument: '--push'"));
    }

    [Test]
    public void Parse_FlagMissingValue_Throws()
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse(["--text"]));

        Assert.That(ex!.Message, Does.Contain("--text requires a value"));
    }

    [TestCase("--help")]
    [TestCase("-h")]
    public void Parse_HelpFlag_ThrowsHelpRequestWithUsageText(string helpFlag)
    {
        var ex = Assert.Throws<CliUsageException>(() =>
            CliOptions.Parse([helpFlag]));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.IsHelpRequest, Is.True);
            Assert.That(ex.Message, Is.EqualTo(CliOptions.Usage));
        });
    }
}
