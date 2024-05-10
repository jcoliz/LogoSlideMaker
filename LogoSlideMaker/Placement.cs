using DocumentFormat.OpenXml.Spreadsheet;
using ShapeCrawler;
using System.Drawing;
using System.Drawing.Imaging;

public class Definition
{
    public Config Config { get; set; } = new();
    public Dictionary<string,Logo> Logos { get; set; } = [];
    public List<Row> Rows { get; set; } = new List<Row>();
}

public class Logo
{
    public string Title { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public double? TextWidth { get; set; }

    public double Scale { get; set; } = 1.0;
}

public class Config
{
    /// <summary>
    /// Vertical distance from middle of icon to top of text, in inches
    /// </summary>
    public double TextDistace { get; set; }

    public double TextWidth { get; set; }
    public double TextHeight { get; set; }

    /// <summary>
    /// Width & height of icon, in inches
    /// </summary>
    public double IconSize { get; set; }

    public double Dpi { get; set; }
}

public class Row
{
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double Width { get; set; }
    public List<string> Logos { get; set; } = new List<string>();
    public int NumItems => Logos.Count;
    public double Spacing => (NumItems > 1) ? Width / ((double)NumItems - 1) : 0;
}

public class Placement(Config config, Row row, Logo logo)
{
    public int Index { get; init; }

    public void RenderTo(ISlideShapes shapes)
    {
        if (Path.GetExtension(logo.Path).ToLowerInvariant() == ".svg")
        {
            var svg = Svg.SvgDocument.Open(logo.Path);
            var svg_aspect = svg.Width.Value / svg.Height.Value;

            // Arbitrarily render the bitmap at 96px (2 inch) high
            svg.Width = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, 2*96f * svg_aspect);
            svg.Height = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, 2*96f);
            var bitmap = svg.Draw();

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0,SeekOrigin.Begin);
            shapes.AddPicture(stream);

        }
        else
        {
            using var stream = new FileStream(logo.Path,FileMode.Open);
            shapes.AddPicture(stream);
        }
        
        var pic = shapes.OfType<IPicture>().Last();

        // Adjust size of icon depending on size of source image. The idea is all
        // icons occupy the same number of pixel area

        var aspect = (double)pic.Width / (double)pic.Height;
        var width_factor = Math.Sqrt(aspect);
        var height_factor = 1.0 / width_factor;
        var icon_width = config.IconSize * width_factor * logo.Scale;
        var icon_height = config.IconSize * height_factor * logo.Scale;

        pic.X = (int)((row.XPosition + Index * row.Spacing - icon_width / 2)*config.Dpi);
        pic.Y = (int)((row.YPosition - icon_height / 2)*config.Dpi);
        pic.Width = (int)(icon_width * config.Dpi);
        pic.Height = (int)(icon_height * config.Dpi);

        var text_width = logo.TextWidth ?? config.TextWidth;

        shapes.AddRectangle(
            x:(int)((row.XPosition + Index * row.Spacing - text_width / 2) * config.Dpi), 
            y:(int)((row.YPosition - config.TextHeight / 2 + config.TextDistace) * config.Dpi), 
            width:(int)(text_width * config.Dpi), height:(int)(config.TextHeight * config.Dpi)
        );

        var shape = shapes.Last();
        var tf = shape.TextFrame;
        tf.Text = logo.Title;
        var font = tf.Paragraphs.First().Portions.First().Font;

        // TODO: All of these should be supplied on config
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
