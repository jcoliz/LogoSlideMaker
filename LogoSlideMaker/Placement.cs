using ShapeCrawler;
using System.Drawing.Imaging;

/// <summary>
/// The overall run definition as loaded in as the top-level toml file
/// </summary>
public record Definition
{
    public Config Config { get; set; } = new();

    /// <summary>
    /// Variations of the logos which each get their own slide
    /// </summary>
    public List<Variant> Variants { get; set; } = new();

    /// <summary>
    /// The actual logos to be displayed, indexed by id
    /// </summary>
    public Dictionary<string,Logo> Logos { get; set; } = [];

    /// <summary>
    /// Rows of logo positions describing how the logos are laid out
    /// </summary>
    /// <remarks>
    /// DEPRECATED, use Boxes
    /// </remarks>
    public List<Row> Rows { get; set; } = new();

    /// <summary>
    /// Multi-line boxes of logo positions describing how the logos are laid out
    /// </summary>
    public List<Box> Boxes { get; set; } = new();

    /// <summary>
    /// All the boxes decomposed into rows
    /// </summary>
    public IEnumerable<Row> AllRows => Rows.Concat(Boxes.SelectMany(x=>x.GetRows(Config.LineSpacing, Config.DefaultWidth)));
}

/// <summary>
/// Describes a variation of logos. One variant will be rendered on each slide.
/// </summary>
public record Variant
{
    /// <summary>
    /// Human-readable name
    /// </summary>
    /// <remarks>
    /// Ideally, would be added to slide notes
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    /// <remarks>
    /// Ideally, would be added to slide notes
    /// </remarks>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Which tags identify logos which should hold a blank space instead of
    /// showing the logo
    /// </summary>
    public List<string> Blank { get; set; } = new();

    /// <summary>
    /// Which tags identify logos which should should be included
    /// </summary>
    /// <remarks>
    /// By default, any logos with tags are excluded from each variant
    /// </remarks>
    public List<string> Include { get; set; } = new();
}

/// <summary>
/// Details about a particular logo glyph
/// </summary>
public record Logo
{
    /// <summary>
    /// Human-readble title to show under the logo
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Where to find the image data
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Tags which describe the logo, used by variants to pick which logos
    /// go in which variant
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Override default text width for this logo
    /// </summary>
    public double? TextWidth { get; set; }

    /// <summary>
    /// Override default scale for this logo
    /// </summary>
    public double Scale { get; set; } = 1.0;
}

public record Config
{
    /// <summary>
    /// Vertical distance from middle of icon to top of text, in inches
    /// </summary>
    public double TextDistace { get; set; }

    /// <summary>
    /// Default width of text under logos, in inches
    /// </summary>

    public double TextWidth { get; set; }

    /// <summary>
    /// Height of text box under logos, in inches
    /// </summary>
    public double TextHeight { get; set; }

    /// <summary>
    /// Width & height of square icons, in inches
    /// </summary>
    public double IconSize { get; set; }

    /// <summary>
    /// Default vertical space between successive lines, in inches
    /// </summary>
    public double LineSpacing { get; set; }

    /// <summary>
    /// Default width of row, in inches
    /// </summary>
    public double? DefaultWidth { get; set; }

    /// <summary>
    /// Dots (pixels) per inch
    /// </summary>
    public double Dpi { get; set; }
}

/// <summary>
/// A method to specify multiple rows in one declaration
/// </summary>
public record Box
{
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double? Width { get; set; }
    public int MinColumns { get; set; }
    public Dictionary<int,List<string>> Logos { get; set; } = new Dictionary<int,List<string>>();

    public IEnumerable<Row> GetRows(double spacing, double? default_width )
    {
        return Logos
            .OrderBy(x=>x.Key)
            .Select((x,i) => new Row() 
            {
                XPosition = XPosition,
                YPosition = YPosition + i * spacing,
                Width = Width ?? default_width ?? throw new ApplicationException("Must specify default with or box width"),
                MinColumns = MinColumns,
                Logos = x.Value
            });
    }
}

public record Row
{
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double Width { get; set; }
    public int MinColumns { get; set; }
    public List<string> Logos { get; set; } = new List<string>();
    public int NumItems => Math.Max( Logos.Count, MinColumns );
    public double Spacing => (NumItems > 1) ? Width / ((double)NumItems - 1) : 0;
}

public enum Commands { Invalid = 0, End = 1 }

/// <summary>
/// Decompose a single logo entry within a row into component parts
/// </summary>
/// <remarks>
/// The `logos=` line in a row can encode a lot of information. This breaks
/// it apart into well-defined symbols.
/// </remarks>
public record Entry
{
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <remarks>
    /// OK to just construct directly, although not commonly used this way
    /// </remarks>
    public Entry() {}

    /// <summary>
    /// Construct from a row logo entry
    /// </summary>
    /// <param name=""></param>
    public Entry(string value)
    {
        var split = value.Split(':');
        var logoId = split[0];

        if (logoId.StartsWith('@'))
        {
            Command = Enum.Parse<Commands>(logoId[1..], ignoreCase:true);
        }
        else
        {
            Id = logoId;
        }

        var tags = split.Skip(1).GroupBy(x=>!x.StartsWith('!')).ToDictionary(x=>x.Key,x=>x);
        Tags = tags.GetValueOrDefault(true)?.ToArray() ?? [];
        NotTags = tags.GetValueOrDefault(false)?.Select(x=>x[1..]).ToArray() ?? [];
    }

    /// <summary>
    /// Logo ID, or null if is not a logo
    /// </summary>
    /// <remarks>
    /// Used to lookup into Definition.Logos
    /// </remarks>
    public string? Id { get; set; }

    /// <summary>
    /// Processing command
    /// </summary>
    /// <remarks>
    /// Set by "@{command}" in row logos
    /// </remarks>
    public Commands? Command { get; set; }

    /// <summary>
    /// Entry-specific tags, will include this entry if match variant
    /// </summary>
    /// <remarks>
    /// Set with "app:tag" in row logos
    /// </remarks>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Entry-specific disincluding tags, will include this entry if does NOT match variant
    /// </summary>
    /// <remarks>
    /// Set with "app:!tag" in row logos
    /// </remarks>
    public string[] NotTags { get; set; } = [];
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
        pic.Y = (int)((row.YPosition - icon_height / 2.0)*config.Dpi);
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
        tf.LeftMargin = 0;
        tf.RightMargin = 0;
        var font = tf.Paragraphs.First().Portions.First().Font;

        // TODO: All of these should be supplied on config
        font.Size = 7;
        font.LatinName = "Segoe UI";
        font.Color.Update("595959");
        shape.Fill.SetColor("FFFFFF");
        shape.Outline.HexColor = "FFFFFF";
    }

}

public class Renderer(Config config, Dictionary<string,Logo> logos, Variant variant, ISlideShapes shapes)
{
    public void Render(Row _row)
    {
        // Skip any logos that aren't included.
        var entries = RowVariant(_row);
        var row = _row with { Logos = entries.Select(x=>x.Id ?? string.Empty).ToList()};

        int i = 0;
        foreach(var entry in entries)
        {
            if (entry.Command == Commands.End)
            {
                break;
            }

            var logo = logos[entry.Id!];

            if (LogoShownInVariant(logo))
            {
                var item = new Placement(config, row, logo) { Index = i };
                item.RenderTo(shapes);
            }

            ++i;
        }
    }

    private bool LogoShownInVariant(Logo logo)
    {
        // Logos with no tags are always shown
        if (logo.Tags.Count == 0)
            return true;

        // Explicitly included logos are always included
        if (logo.Tags.Intersect(variant.Include).Any())
            return true;

        return false;
    }

    private bool EntryIncludedInVariant(Entry entry)
    {
        // Split out "not" tags, where the logo is included if the tag
        // is NOT in the variant

        var tags = entry.Tags.ToList();

        // Lookup tags from logo if there is a logo
        if (entry.Id != null)
        {
            var logo = logos[entry.Id!];

            // Also include placement-only tags which are included in the
            // id with an at-sign,
            // e.g. "app@tag"
            tags.AddRange( logo.Tags );
        }

        // Logos with 'not' tags are excluded if variant includes the tag
        if (entry.NotTags.Any())
        {
            if (entry.NotTags.Intersect(variant.Include).Any())
                return false;
        } 
        
        // Logos with no tags are always included
        if (tags.Count == 0)
            return true;

        // Blanked logos are included at this stage
        if (tags.Intersect(variant.Blank).Any())
            return true;

        // Explicitly included logos are always included
        if (tags.Intersect(variant.Include).Any())
            return true;

        // Otherwise, logos with tags are excluded by default
        return false;
    }

    /// <summary>
    /// Transform to entries, and filter out non-included entries
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    private ICollection<Entry> RowVariant(Row row)
    {
        return row.Logos.Select(x=>new Entry(x)).Where(x => EntryIncludedInVariant(x)).ToArray();
    }
}
