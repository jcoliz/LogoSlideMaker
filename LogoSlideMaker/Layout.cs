using System.Collections.Immutable;
using DocumentFormat.OpenXml.Wordprocessing;
using ShapeCrawler;

/// <summary>
/// A collection of icons which have already been chosen and placed
/// </summary>
public class Layout(Definition definition, Variant variant): List<BoxLayout>
{
    private readonly string Name = variant.Name;
    private readonly IEnumerable<string> Description = variant.Description;
    public void PopulateFrom()
    {
        // Add well-defined boxes
        base.AddRange
        (
            definition.Boxes.Aggregate<Box,List<BoxLayout>>(new(), LayoutAggregateBox)
        );

        // Add loose rows
        base.AddRange
        (
            definition.Rows.Select(x => new BoxLayout() { Logos = LayoutRow(x).ToArray() })
        );
    }

    private List<BoxLayout> LayoutAggregateBox(List<BoxLayout> layouts, Box box)
    {
        // Fill in missing box.YPosition if it has none
        decimal YPosition = 0m;
        if (box.YPosition is null)
        {
            var last = layouts.LastOrDefault();
            if (last is null)
            {
                throw new ApplicationException("Must set explicit YPosition on first box");
            }
            YPosition = last.Logos.Max(x=>x.Y) + definition.Config.LineSpacing + definition.Config.BoxSpacing;
        }

        layouts.Add( LayoutBox(box, YPosition) );
        return layouts;
    }

    private BoxLayout LayoutBox(Box box, decimal YPosition)
    {
        var logos = box.Logos
            .OrderBy(x=>x.Key)
            .Select((x,i) => new Row() 
            {
                XPosition = box.XPosition,
                YPosition = (box.YPosition ?? YPosition) + i * definition.Config.LineSpacing,
                Width = box.Width ?? definition.Config.DefaultWidth ?? throw new ApplicationException("Must specify default with or box width"),
                MinColumns = box.MinColumns,
                Logos = x.Value
            })
            .SelectMany(x => LayoutRow(x));

        return new BoxLayout() { Heading = box.Title, Logos = logos.ToArray() };
    }

    public void RenderTo(ISlideShapes shapes)
    {
        var config = definition.Config;

        if (config.Listing)
        {   
            Console.WriteLine();
            Console.WriteLine($"## {Name}");
            Console.WriteLine();
            foreach(var line in Description)
            {
                Console.WriteLine(line);        
            }
        }

        // Fill in description field
        if (variant.Description.Count > 0)
        {
            var description_box = shapes.TryGetByName<IShape>("Description");
            if (description_box is not null)
            {
                var tf = description_box.TextFrame;
                var maxlines = Math.Min(Description.Count(),tf.Paragraphs.Count);
                for (int l = 0; l < maxlines; l++)
                {
                    tf.Paragraphs[l].Text = Description.Skip(l).First();
                }
            }
        }

        foreach(var boxlayout in this)
        {
            if (config.Listing)
            {   
                Console.WriteLine();
                Console.WriteLine($"### {boxlayout.Heading}");
                Console.WriteLine();
            }

            foreach(var logolayout in boxlayout.Logos)
            {                
                var logo = logolayout.Logo;

                if (logo is null)
                {
                    continue;
                }

                if (config.Listing)
                {
                    string alt_text = string.IsNullOrWhiteSpace(logo.AltText) ? string.Empty : $"{logo.AltText} ";
                    Console.WriteLine($"* {alt_text}{logo.Title}");
                }

                {
                    using var stream = new FileStream(logo.Path,FileMode.Open);
                    shapes.AddPicture(stream);
                }
                
                var pic = shapes.OfType<IPicture>().Last();

                // Adjust size of icon depending on size of source image. The idea is all
                // icons occupy the same number of pixel area

                var aspect = pic.Width / pic.Height;
                var width_factor = (decimal)Math.Sqrt((double)aspect);
                var height_factor = 1.0m / width_factor;
                var icon_width = config.IconSize * width_factor * logo.Scale;
                var icon_height = config.IconSize * height_factor * logo.Scale;

                pic.X = ( logolayout.X - icon_width / 2.0m ) * config.Dpi;
                pic.Y = ( logolayout.Y - icon_height / 2.0m ) * config.Dpi;
                pic.Width = icon_width * config.Dpi;
                pic.Height = icon_height * config.Dpi;

                var text_width_inches = logo.TextWidth ?? config.TextWidth;

                decimal text_x = ( logolayout.X - text_width_inches / 2.0m ) * config.Dpi;
                decimal text_y = ( logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi;
                decimal text_width = text_width_inches * config.Dpi;
                decimal text_height = config.TextHeight * config.Dpi;

                shapes.AddRectangle(100,100,100,100);
                var shape = shapes.Last();

                shape.X = text_x;
                shape.Y = text_y;
                shape.Width = text_width;
                shape.Height = text_height;

                var tf = shape.TextFrame;
                tf.Text = logo.Title;
                tf.LeftMargin = 0;
                tf.RightMargin = 0;
                var font = tf.Paragraphs.First().Portions.First().Font;

                font.Size = config.FontSize;
                font.LatinName = config.FontName;
                font.Color.Update(config.FontColor);
                shape.Fill.SetNoFill(); //SetColor(config.BackgroundColor);
                shape.Outline.Weight = 0;
                shape.Outline.HexColor = config.BackgroundColor;            
            }
        }

    }

    private IEnumerable<LogoLayout> LayoutRow(Row _row)
    {
        var result = new List<LogoLayout>();

        // Skip any logos that aren't included.
        var entries = RowVariant(_row);
        var row = _row with { Logos = entries.Select(x=>x.Id ?? string.Empty).ToList()};

        int column = 0;
        foreach(var entry in entries)
        {
            if (entry.Command == Commands.End)
            {
                break;
            }

            var logo = definition.Logos[entry.Id!];

            if (LogoShownInVariant(logo))
            {
                // TODO: Return this
                result.Add(new LogoLayout() { Logo = logo, X = row.XPosition + column * row.Spacing , Y = row.YPosition });
            }

            ++column;
        }

        if (result.Count == 0)
        {
            // Ensure there is at least one logolayout, even if empty, to hold space for this row.
            result.Add( new LogoLayout() { Y = row.YPosition } );
        }

        return result;
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

    private bool EntryIncludedInVariant(Entry entry)
    {
        // Start with the tags explicitly specified on this entry
        var tags = entry.Tags.ToList();

        // Add tags from logo if there is a logo
        if (entry.Id != null)
        {
            var logo = definition.Logos[entry.Id];

            // Also include placement-only tags which are included in the
            // id with an at-sign,
            // e.g. "app@tag"
            tags.AddRange( logo.Tags );

            // TODO: Note that a logo definition cannot declare a "not"
            // tag. That is only used in entry placements. We COULD add
            // that in the future. It would go here.
        }

        // Entries with 'not' tags are excluded if variant includes the tag
        if (entry.NotTags.Intersect(variant.Include).Any())
            return false;
        
        // Entries with no tags are always included
        if (tags.Count == 0)
            return true;

        // Blanked Entries are included at this stage
        // Explicitly included Entries are always included
        if (tags.Intersect(variant.Include.Union(variant.Blank)).Any())
            return true;

        // Otherwise, Entries with tags are excluded by default
        return false;
    }

    private bool LogoShownInVariant(Logo logo)
    {
        // Logos with no tags are always shown
        if (logo.Tags.Count == 0)
            return true;

        // Tags on the "blank" list are not shown
        if (logo.Tags.Intersect(variant.Blank).Any())
            return false;

        // Explicitly included logos are always included
        if (logo.Tags.Intersect(variant.Include).Any())
            return true;

        return false;
    }

}

/// <summary>
/// A single logo with positioning
/// </summary>
public record LogoLayout
{
    public Logo? Logo { get; init; }
    public decimal X { get; init; }
    public decimal Y { get; init; }
}

public record BoxLayout
{
    public string? Heading { get; init; }
    public LogoLayout[] Logos { get; init; } = [];
}