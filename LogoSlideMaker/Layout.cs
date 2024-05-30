/// <summary>
/// A collection of icons which have already been chosen and placed
/// </summary>
public class Layout(Definition definition, Variant variant): List<BoxLayout>, ILayout
{
    public string Name { get; } = variant.Name;
    public IEnumerable<string> Description { get; } = variant.Description;

    public void Populate()
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

    /// <summary>
    /// Layout a single box, considering the state of already-laid-out boxes
    /// </summary>
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
            YPosition = last.Logos.Max(x=>x.Y) + definition.Layout.LineSpacing + definition.Layout.BoxSpacing;
        }

        layouts.Add( LayoutBox(box, YPosition) );
        return layouts;
    }

    /// <summary>
    /// Layout a single box
    /// </summary>
    /// <param name="YPosition">
    /// Default yposition to use if box has no intrinsic position defined
    /// </param>
    private BoxLayout LayoutBox(Box box, decimal YPosition)
    {
        var logos = box.Logos
            .OrderBy(x=>x.Key)
            .Select((x,i) => new Row() 
            {
                XPosition = box.XPosition,
                YPosition = (box.YPosition ?? YPosition) + i * definition.Layout.LineSpacing,
                Width = box.Width ?? definition.Layout.DefaultWidth ?? throw new ApplicationException("Must specify default with or box width"),
                MinColumns = box.MinColumns,
                Logos = x.Value
            })
            .SelectMany(x => LayoutRow(x));

        return new BoxLayout() { Heading = box.Title, Logos = logos.ToArray() };
    }

    private IEnumerable<LogoLayout> LayoutRow(Row _row)
    {
        var result = new List<LogoLayout>();

        // Skip any logos that aren't included.
        var entries = FilteredRowEntries(_row);
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
    private ICollection<Entry> FilteredRowEntries(Row row)
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