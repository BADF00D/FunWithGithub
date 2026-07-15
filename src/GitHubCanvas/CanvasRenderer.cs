namespace GitHubCanvas;

/// <summary>A single lit pixel of the canvas, mapped to the calendar date it represents.</summary>
internal readonly record struct CanvasPixel(int Row, int Column, DateOnly Date);

/// <summary>
/// Renders a text string through <see cref="PixelFont"/> onto the 7-row GitHub
/// contribution grid and maps every lit pixel to the calendar date it corresponds to.
/// </summary>
internal static class CanvasRenderer
{
    public const int Rows = PixelFont.GlyphHeight;
    public const int MaxColumns = 52;

    public static int CalculateWidth(string text) =>
        text.Length == 0 ? 0 : text.Length * (PixelFont.GlyphWidth + PixelFont.GlyphSpacing) - PixelFont.GlyphSpacing;

    /// <summary>
    /// Renders <paramref name="text"/> starting at <paramref name="startDate"/> (must be a Sunday)
    /// and returns every lit pixel together with the calendar date it maps to.
    /// Column 0 = the week of <paramref name="startDate"/>, row 0 = Sunday, row 6 = Saturday.
    /// </summary>
    public static IReadOnlyList<CanvasPixel> Render(string text, DateOnly startDate)
    {
        var pixels = new List<CanvasPixel>();
        var column = 0;

        foreach (var c in text)
        {
            for (var glyphCol = 0; glyphCol < PixelFont.GlyphWidth; glyphCol++)
            {
                for (var row = 0; row < Rows; row++)
                {
                    if (PixelFont.IsPixelLit(c, row, glyphCol))
                    {
                        var date = startDate.AddDays(column * 7 + row);
                        pixels.Add(new CanvasPixel(row, column, date));
                    }
                }

                column++;
            }

            column += PixelFont.GlyphSpacing;
        }

        return pixels;
    }
}
