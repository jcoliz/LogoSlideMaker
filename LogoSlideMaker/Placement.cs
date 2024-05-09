using ShapeCrawler;
using System.Drawing;
using System.Drawing.Imaging;

public record Logo
{
    public string Title { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

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

public record Row
{
    public double XPosition { get; init; }
    public double YPosition { get; init; }
    public double Width { get; init; }
    public List<string> Logos { get; init; } = new();
    public int NumItems => Logos.Count;
    public double Spacing => Width / ((double)NumItems - 1);
}

public class Placement(Config config, Row row, Logo logo)
{
    public int Index { get; init; }

    public void RenderTo(ISlideShapes shapes)
    {
        /*
        shapes.AddRectangle(
            x:(int)((row.XPosition + Index * row.Spacing - config.IconSize / 2)*config.Dpi), 
            y:(int)((row.YPosition - config.IconSize / 2)*config.Dpi), 
            width:(int)(config.IconSize * config.Dpi), 
            height:(int)(config.IconSize * config.Dpi)
        );
        */

        //Image image = Image.FromFile("wine-svgrepo-com.svg");
        //var stream = new MemoryStream();
        //image.Save(stream, image.RawFormat);
        //stream.Position = 0;

        using var stream = new FileStream(logo.Path,FileMode.Open);
        shapes.AddPicture(stream);
        
        //using var stream = File.OpenRead("wine-svgrepo-com.svg");
        //shapes.AddPicture(stream);

        var shape = shapes.Last();
        shape.X = (int)((row.XPosition + Index * row.Spacing - config.IconSize / 2)*config.Dpi);
        shape.Y = (int)((row.YPosition - config.IconSize / 2)*config.Dpi);
        shape.Width = (int)(config.IconSize * config.Dpi);
        shape.Height = (int)(config.IconSize * config.Dpi);

        shapes.AddRectangle(
            x:(int)((row.XPosition + Index * row.Spacing - config.TextWidth / 2) * config.Dpi), 
            y:(int)((row.YPosition - config.TextHeight / 2 + config.TextDistace) * config.Dpi), 
            width:(int)(config.TextWidth * config.Dpi), height:(int)(config.TextHeight * config.Dpi)
        );

        shape = shapes.Last();
        var tf = shape.TextFrame;
        tf.Text = logo.Title;
        var font = tf.Paragraphs.First().Portions.First().Font;
        font.Size = 7;
        font.LatinName = "Segoe UI";
        font.Color.Update("595959");
        shape.Fill.SetColor("FFFFFF");
        shape.Outline.HexColor = "FFFFFF";
    }

}

public class Renderer(Config config, Dictionary<string,Logo> logos, ISlideShapes shapes)
{
    public void Render(Row row)
    {
        for(int i = 0; i < row.NumItems; ++i )
        {
            var item = new Placement(config, row, logos[row.Logos[i]]) { Index = i };
            item.RenderTo(shapes);
        }
    }
}
