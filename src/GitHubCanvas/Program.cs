using GitHubCanvas;

try
{
    var options = CliOptions.Parse(args);
    var repoPath = Directory.GetCurrentDirectory();

    var creator = new GitCommitCreator(repoPath, options.Author);
    creator.EnsurePreconditions();

    var width = CanvasRenderer.CalculateWidth(options.Text);
    var pixels = CanvasRenderer.Render(options.Text, options.StartDate)
        .OrderBy(p => p.Date)
        .ToList();

    PrintPreview(options.Text, options.StartDate, width, pixels, options.CommitsPerDate);

    if (pixels.Count == 0)
    {
        Console.WriteLine("Nothing to draw (text renders to zero lit pixels). Exiting.");
        return 0;
    }

    var totalCommits = pixels.Count * options.CommitsPerDate;
    Console.Write($"Proceed and create {totalCommits} commit(s) in '{repoPath}'? [y/N]: ");
    var answer = Console.ReadLine()?.Trim();
    if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Aborted. No commits were created.");
        return 1;
    }

    var commitMessage = $"GitHub canvas art: \"{options.Text}\"";
    var done = 0;

    foreach (var pixel in pixels)
    {
        for (var i = 0; i < options.CommitsPerDate; i++)
        {
            creator.CreateCommit(pixel.Date, i, commitMessage);
            done++;
            Console.Write($"\rCreating commits... {done}/{totalCommits}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Done. Created {totalCommits} commit(s) on the current branch. Nothing has been pushed yet.");
    return 0;
}
catch (CliUsageException ex)
{
    Console.WriteLine(ex.Message);
    return ex.IsHelpRequest ? 0 : 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintPreview(string text, DateOnly startDate, int width, IReadOnlyList<CanvasPixel> pixels, int commitsPerDate)
{
    var grid = new bool[CanvasRenderer.Rows, width];
    foreach (var pixel in pixels)
    {
        grid[pixel.Row, pixel.Column] = true;
    }

    string[] dayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    Console.WriteLine();
    Console.WriteLine($"Preview for \"{text}\" starting {startDate:yyyy-MM-dd}:");
    Console.WriteLine();

    for (var row = 0; row < CanvasRenderer.Rows; row++)
    {
        Console.Write($"{dayLabels[row]} ");
        for (var col = 0; col < width; col++)
        {
            if (grid[row, col])
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write('■');
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write('·');
                Console.ResetColor();
            }
        }

        Console.WriteLine();
    }

    var endDate = width == 0 ? startDate : startDate.AddDays((width - 1) * 7 + 6);
    Console.WriteLine();
    Console.WriteLine($"Grid width: {width} column(s) ({startDate:yyyy-MM-dd} .. {endDate:yyyy-MM-dd})");
    Console.WriteLine(
        $"Lit pixels: {pixels.Count}, commits per pixel: {commitsPerDate}, total commits: {pixels.Count * commitsPerDate}");
    Console.WriteLine();
}
