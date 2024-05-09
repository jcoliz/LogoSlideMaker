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

        // By default, icons are square
        var icon_width = config.IconSize;
        var icon_height = config.IconSize;

        if (Path.GetExtension(logo.Path).ToLowerInvariant() == ".svg")
        {
            var svg = Svg.SvgDocument.Open(logo.Path);
            var aspect = svg.Width.Value / svg.Height.Value;

            // Arbitrarily render the bitmap at 96px (2 inch) high
            svg.Width = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, 2*96f * aspect);
            svg.Height = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, 2*96f);
            var bitmap = svg.Draw();

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0,SeekOrigin.Begin);
            shapes.AddPicture(stream);

            // Adjust size of icon depending on size of source image. The idea is all
            // icons occupy the same number of pixel area

            var width_factor = Math.Sqrt(aspect);
            var height_factor = 1.0 / width_factor;

            icon_width *= width_factor;
            icon_height *= height_factor;
        }
        else
        {
            using var stream = new FileStream(logo.Path,FileMode.Open);
            shapes.AddPicture(stream);
        }
        
        //using var stream = File.OpenRead("wine-svgrepo-com.svg");
        //shapes.AddPicture(stream);

        IShape shape = shapes.Last();
        shape.X = (int)((row.XPosition + Index * row.Spacing - icon_width / 2)*config.Dpi);
        shape.Y = (int)((row.YPosition - icon_height / 2)*config.Dpi);
        shape.Width = (int)(icon_width * config.Dpi);
        shape.Height = (int)(icon_height * config.Dpi);

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
