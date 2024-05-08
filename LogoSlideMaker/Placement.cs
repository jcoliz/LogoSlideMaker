using ShapeCrawler;

public record Config
{
    /// <summary>
    /// Vertical distance from middle of icon to top of text, in inches
    /// </summary>
    public double TextDistace { get; init; }

    public double TextWidth { get; init; }
    public double TextHeight { get; init; }

    /// <summary>
    /// Width & height of icon, in inches
    /// </summary>
    public double IconSize { get; init; }

    public double Dpi { get; init; }
}

public class Row(Config config)
{
    public double XPosition { get; init; }
    public double YPosition { get; init; }
    public double Width { get; init; }
    public int NumItems { get; init; }
    public double Spacing => Width / ((double)NumItems - 1);

    public void RenderTo(ISlideShapes shapes)
    {
        for(int i = 0; i < NumItems; ++i )
        {
            var item = new Placement(config, this) { Index = i };
            item.RenderTo(shapes);
        }
    }
}

public class Placement(Config config, Row row)
{
    public int Index { get; init; }

    public void RenderTo(ISlideShapes shapes)
    {
        shapes.AddRectangle(
            x:(int)((row.XPosition + Index * row.Spacing - config.IconSize / 2)*config.Dpi), 
            y:(int)((row.YPosition - config.IconSize / 2)*config.Dpi), 
            width:(int)(config.IconSize * config.Dpi), 
            height:(int)(config.IconSize * config.Dpi)
        );

        shapes.AddRectangle(
            x:(int)((row.XPosition + Index * row.Spacing - config.TextWidth / 2) * config.Dpi), 
            y:(int)((row.YPosition - config.TextHeight / 2 + config.TextDistace) * config.Dpi), 
            width:(int)(config.TextWidth * config.Dpi), height:(int)(config.TextHeight * config.Dpi)
        );

        var shape = shapes.Last();
        var tf = shape.TextFrame;
        tf.Text = $"Hello, Icon #{Index}!";
        var font = tf.Paragraphs.First().Portions.First().Font;
        font.Size = 7;
        font.LatinName = "Segoe UI";
        font.Color.Update("595959");
        shape.Fill.SetColor("FFFFFF");
        shape.Outline.HexColor = "FFFFFF";
    }
}
