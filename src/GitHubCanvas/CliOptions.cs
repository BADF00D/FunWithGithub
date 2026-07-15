using System.Globalization;
using System.Text.RegularExpressions;

namespace GitHubCanvas;

internal sealed record GitAuthor(string Name, string Email);

internal sealed record CliOptions(
    string Text,
    DateOnly StartDate,
    int CommitsPerDate,
    GitAuthor? Author)
{
    public const string Usage = """
        Usage: githubcanvas --text <TEXT> --start-date <yyyy-MM-dd> [--commits-per-date <N>] [--author "Name <email>"]

        Options:
          --text <TEXT>                Text to render onto the contribution graph (A-Z, 0-9, space).
          --start-date <yyyy-MM-dd>    Calendar date for column 0 of the grid. Must be a Sunday.
          --commits-per-date <N>       Number of commits to create per lit pixel (default: 1).
          --author "Name <email>"      Author/committer identity for the generated commits.
                                        If omitted, the repository's configured git identity is used.
          -h, --help                   Show this help text.
        """;

    /// <summary>Parses and validates raw CLI arguments. Throws <see cref="CliUsageException"/> on any problem.</summary>
    public static CliOptions Parse(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help"))
        {
            throw new CliUsageException(Usage, isHelpRequest: true);
        }

        string? text = null;
        string? startDateRaw = null;
        string? commitsPerDateRaw = null;
        string? authorRaw = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--text":
                    text = RequireValue(args, ref i, "--text");
                    break;
                case "--start-date":
                    startDateRaw = RequireValue(args, ref i, "--start-date");
                    break;
                case "--commits-per-date":
                    commitsPerDateRaw = RequireValue(args, ref i, "--commits-per-date");
                    break;
                case "--author":
                    authorRaw = RequireValue(args, ref i, "--author");
                    break;
                default:
                    throw new CliUsageException($"Unknown argument: '{args[i]}'\n\n{Usage}");
            }
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new CliUsageException($"--text is required and must not be empty.\n\n{Usage}");
        }

        var unsupportedChars = text.Where(c => !PixelFont.IsSupported(c)).Distinct().ToList();
        if (unsupportedChars.Count > 0)
        {
            var chars = string.Join(", ", unsupportedChars.Select(c => $"'{c}'"));
            throw new CliUsageException(
                $"Unsupported character(s) in --text: {chars}. Only A-Z, 0-9 and space are supported.");
        }

        var width = CanvasRenderer.CalculateWidth(text);
        if (width > CanvasRenderer.MaxColumns)
        {
            throw new CliUsageException(
                $"--text is too wide: '{text}' needs {width} columns, but a year only offers " +
                $"{CanvasRenderer.MaxColumns} weeks. Use shorter text.");
        }

        if (string.IsNullOrWhiteSpace(startDateRaw))
        {
            throw new CliUsageException($"--start-date is required.\n\n{Usage}");
        }

        if (!DateOnly.TryParseExact(startDateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var startDate))
        {
            throw new CliUsageException($"--start-date '{startDateRaw}' is not a valid date. Expected format: yyyy-MM-dd.");
        }

        if (startDate.DayOfWeek != DayOfWeek.Sunday)
        {
            var previousSunday = startDate.AddDays(-(int)startDate.DayOfWeek);
            var nextSunday = previousSunday.AddDays(7);
            throw new CliUsageException(
                $"--start-date '{startDateRaw}' ({startDate.DayOfWeek}) must be a Sunday, since GitHub weeks " +
                $"start on Sunday. Nearest Sundays: {previousSunday:yyyy-MM-dd} or {nextSunday:yyyy-MM-dd}.");
        }

        var commitsPerDate = 1;
        if (!string.IsNullOrWhiteSpace(commitsPerDateRaw))
        {
            if (!int.TryParse(commitsPerDateRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out commitsPerDate)
                || commitsPerDate < 1)
            {
                throw new CliUsageException($"--commits-per-date '{commitsPerDateRaw}' must be a positive integer.");
            }
        }

        GitAuthor? author = null;
        if (!string.IsNullOrWhiteSpace(authorRaw))
        {
            var match = Regex.Match(authorRaw, @"^(?<name>.+?)\s*<(?<email>[^<>]+)>$");
            if (!match.Success)
            {
                throw new CliUsageException(
                    $"--author '{authorRaw}' must be in the form \"Name <email>\".");
            }

            author = new GitAuthor(match.Groups["name"].Value, match.Groups["email"].Value);
        }

        return new CliOptions(text, startDate, commitsPerDate, author);
    }

    private static string RequireValue(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length)
        {
            throw new CliUsageException($"{flag} requires a value.");
        }

        return args[++i];
    }
}

/// <summary>Thrown for invalid CLI usage. The message is meant to be shown to the user as-is.</summary>
internal sealed class CliUsageException(string message, bool isHelpRequest = false) : Exception(message)
{
    public bool IsHelpRequest { get; } = isHelpRequest;
}
