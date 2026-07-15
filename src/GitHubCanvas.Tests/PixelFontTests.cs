namespace GitHubCanvas.Tests;

[TestFixture]
public class PixelFontTests
{
    [TestCase('A')]
    [TestCase('Z')]
    [TestCase('0')]
    [TestCase('9')]
    [TestCase(' ')]
    public void IsSupported_ReturnsTrue_ForKnownCharacters(char c)
    {
        Assert.That(PixelFont.IsSupported(c), Is.True);
    }

    [TestCase('a')]
    [TestCase('z')]
    public void IsSupported_IsCaseInsensitive(char c)
    {
        Assert.That(PixelFont.IsSupported(c), Is.True);
    }

    [TestCase('!')]
    [TestCase('Ä')]
    [TestCase('.')]
    [TestCase('-')]
    public void IsSupported_ReturnsFalse_ForUnknownCharacters(char c)
    {
        Assert.That(PixelFont.IsSupported(c), Is.False);
    }

    [Test]
    public void IsPixelLit_MatchesExpectedGlyph_ForLetterI()
    {
        // 'I' glyph: top and bottom bar lit across all 5 columns, middle only the center column.
        Assert.Multiple(() =>
        {
            for (var col = 0; col < PixelFont.GlyphWidth; col++)
            {
                Assert.That(PixelFont.IsPixelLit('I', 0, col), Is.True, $"row 0, col {col} should be lit");
                Assert.That(PixelFont.IsPixelLit('I', 6, col), Is.True, $"row 6, col {col} should be lit");
            }

            for (var row = 1; row < 6; row++)
            {
                Assert.That(PixelFont.IsPixelLit('I', row, 2), Is.True, $"row {row}, col 2 should be lit");
                Assert.That(PixelFont.IsPixelLit('I', row, 0), Is.False, $"row {row}, col 0 should be off");
                Assert.That(PixelFont.IsPixelLit('I', row, 4), Is.False, $"row {row}, col 4 should be off");
            }
        });
    }

    [Test]
    public void IsPixelLit_IsCaseInsensitive()
    {
        for (var row = 0; row < PixelFont.GlyphHeight; row++)
        {
            for (var col = 0; col < PixelFont.GlyphWidth; col++)
            {
                Assert.That(PixelFont.IsPixelLit('i', row, col), Is.EqualTo(PixelFont.IsPixelLit('I', row, col)));
            }
        }
    }

    [Test]
    public void IsPixelLit_SpaceGlyph_HasNoLitPixels()
    {
        for (var row = 0; row < PixelFont.GlyphHeight; row++)
        {
            for (var col = 0; col < PixelFont.GlyphWidth; col++)
            {
                Assert.That(PixelFont.IsPixelLit(' ', row, col), Is.False);
            }
        }
    }
}
