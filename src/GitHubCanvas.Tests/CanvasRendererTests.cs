namespace GitHubCanvas.Tests;

[TestFixture]
public class CanvasRendererTests
{
    [TestCase("", 0)]
    [TestCase("A", 5)]
    [TestCase("AB", 11)]
    [TestCase("ABC", 17)]
    public void CalculateWidth_AccountsForGlyphWidthAndSpacing(string text, int expectedWidth)
    {
        Assert.That(CanvasRenderer.CalculateWidth(text), Is.EqualTo(expectedWidth));
    }

    [Test]
    public void Render_EmptyText_ReturnsNoPixels()
    {
        var pixels = CanvasRenderer.Render("", new DateOnly(2026, 1, 4));

        Assert.That(pixels, Is.Empty);
    }

    [Test]
    public void Render_SingleLetterI_ProducesExpectedPixelCountAndDates()
    {
        var startDate = new DateOnly(2026, 1, 4); // a Sunday
        var pixels = CanvasRenderer.Render("I", startDate);

        // top bar (5) + bottom bar (5) + 5 middle-column pixels (rows 1-5) = 15
        Assert.That(pixels, Has.Count.EqualTo(15));

        Assert.Multiple(() =>
        {
            Assert.That(pixels, Has.Some.Matches<CanvasPixel>(p => p is { Row: 0, Column: 0 } && p.Date == startDate));
            Assert.That(pixels, Has.Some.Matches<CanvasPixel>(p =>
                p is { Row: 0, Column: 4 } && p.Date == startDate.AddDays(4 * 7)));
            Assert.That(pixels, Has.Some.Matches<CanvasPixel>(p =>
                p is { Row: 1, Column: 2 } && p.Date == startDate.AddDays(2 * 7 + 1)));
            Assert.That(pixels, Has.Some.Matches<CanvasPixel>(p =>
                p is { Row: 6, Column: 0 } && p.Date == startDate.AddDays(6)));
            Assert.That(pixels, Has.Some.Matches<CanvasPixel>(p =>
                p is { Row: 6, Column: 4 } && p.Date == startDate.AddDays(4 * 7 + 6)));

            // no pixel lit outside the center column in the middle rows
            Assert.That(pixels, Has.None.Matches<CanvasPixel>(p => p.Row is > 0 and < 6 && p.Column != 2));
        });
    }

    [Test]
    public void Render_TwoLetters_SkipsSpacingColumnAndProducesRegressionCount()
    {
        var startDate = new DateOnly(2026, 1, 4);
        var pixels = CanvasRenderer.Render("HI", startDate);

        // Regression check against a manually verified real run (see conversation history).
        Assert.That(pixels, Has.Count.EqualTo(32));

        // Column 5 is the single-column spacer between 'H' (cols 0-4) and 'I' (cols 6-10): must stay dark.
        Assert.That(pixels, Has.None.Matches<CanvasPixel>(p => p.Column == 5));
    }

    [Test]
    public void Render_PixelDate_IsAlwaysStartDatePlusColumnWeeksPlusRowDays()
    {
        var startDate = new DateOnly(2026, 1, 4);
        var pixels = CanvasRenderer.Render("HI", startDate);

        Assert.That(pixels, Is.All.Matches<CanvasPixel>(p => p.Date == startDate.AddDays(p.Column * 7 + p.Row)));
    }
}
