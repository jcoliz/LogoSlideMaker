using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Layout;

/// <summary>
/// A collection of icons which have already been chosen and placed
/// </summary>
public class Layout(Definition definition, Variant variant): List<BoxLayout>, ILayout
{
    public string Name { get; } = variant.Name;
    public IEnumerable<string> Description { get; } = variant.Description;
    public int Source { get; } = variant.Source;

    public void Populate()
    {
        // Add well-defined boxes
        base.AddRange
        (
            definition.Boxes
                .Where(x=>BoxIncludedInVariant(x))
                .Aggregate<Box,List<BoxLayout>>(new(), LayoutAggregateBox)
        );

        // Add loose rows, only if pages are not specified
        if (variant.Pages.Count == 0)
        {
            base.AddRange
            (
                definition.Rows.Select(x => new BoxLayout() { Logos = LayoutRow(x).ToArray() })
            );
        }
    }

    /// <summary>
    /// Layout a single box, considering the state of already-laid-out boxes
    /// </summary>
    private List<BoxLayout> LayoutAggregateBox(List<BoxLayout> layouts, Box box)
    {
        // Fill in missing box.YPosition if it has none
        decimal YPosition = 0m;
        if (box.YPosition is null && box.Outer?.Y is null)
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
            .OrderBy(x => x.Key)
            .Select(x => x.Value);

        var flow = box.AutoFlow ? AutoFlow(logos) : logos!;

        var layouts = flow
            .Select(MakeRow(box,YPosition))
            .SelectMany(x => LayoutRow(x));

        return new BoxLayout() { Heading = box.Title, Logos = layouts.ToArray() };
    }

    /// <summary>
    /// Reflow logos into matching-length rows
    /// </summary>
    private IEnumerable<List<string>> AutoFlow(IEnumerable<List<string>> logos)
    {
        // Initially, we have this many rows
        var num_rows = logos.Count();

        // We have this many logos
        var num_logos = logos.Sum(x=>x.Count);

        // Ergo, we need this many columns
        var num_cols = num_logos / num_rows + (num_logos % num_rows > 0 ? 1 : 0);

        var result = new List<List<string>>();
        var current = new List<string>();
        var current_col = 0;
        // Reflow the logos into the correct columns
        foreach(var logo in logos.SelectMany(x=>x))
        {
            current.Add(logo);
            if (++ current_col >= num_cols)
            {
                result.Add(current);
                current = new List<string>();
                current_col = 0;
            }
        }
        if (current.Count > 0)
        {
            result.Add(current);
        }

        return result;
    }


    private Func<List<string>,int,Row> MakeRow(Box box, decimal YPosition)
    {
        Rectangle inner 
            = (box.Outer is null)
            ? new Rectangle() 
                { 
                    X = box.XPosition ?? 0, // throw new ApplicationException("Must specify explicit X position or outer dimensions"),
                    Y = box.YPosition ?? YPosition,
                    Width = box.Width ?? definition.Layout.DefaultWidth ?? throw new ApplicationException("Must specify default with or box width")
                } 
            : new Rectangle()
            {
                X = box.Outer.X + (definition.Layout.Padding ?? 0) + definition.Render.TextWidth / 2m,
                Y = (box.Outer.Y is not null) ? box.Outer.Y + (definition.Layout.Padding ?? 0) + definition.Render.IconSize / 2m : YPosition,
                Width = box.Outer.Width - (definition.Layout.Padding ?? 0) * 2 - definition.Render.TextWidth
            };

        return (List<string> logos, int col) => new Row() 
            {
                XPosition = inner.X,
                YPosition = (inner.Y ?? 0) + col * definition.Layout.LineSpacing,
                Width = inner.Width,
                MinColumns = box.MinColumns,
                Logos = logos
            };
    }

    private IEnumerable<LogoLayout> LayoutRow(Row _row)
    {
        // Filter down to only included logos.
        var entries = IncludedRowEntries(_row);
        var row = _row with { Logos = entries.Select(x=>x.Id ?? string.Empty).ToList()};

        var layout = entries
            .TakeWhile(x=>x.Command != Commands.End)
            .Select((x,i)=>(logo:definition.Logos[x.Id!],column:i))
            .Where(x=> LogoShownInVariant(x.logo))
            .Select(x=>new LogoLayout() { Logo = x.logo, X = row.XPosition + x.column * row.Spacing , Y = row.YPosition });

        // If row ends up being empty, we still need to placehold vertical space for it
        return layout.Any() ? layout : [ new() { Y = row.YPosition } ];
    }

    /// <summary>
    /// Transform to entries, and filter out non-included entries
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    private ICollection<Entry> IncludedRowEntries(Row row)
    {
        return row.Logos
            .Select(x=>new Entry(x))
            .Where(EntryIncludedInVariant)
            .Select(MaskedIfNeeded)
            .ToArray();
    }

    private Entry MaskedIfNeeded(Entry entry)
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

        if (variant.Mask is not null && tags.Intersect(variant.Mask.Tags).Any())
        {
            return entry with { Id = variant.Mask.Logo };
        }
        else
        {
            return entry;
        }

    }

    /// <summary>
    /// True to show the logo OR hold space for it
    /// </summary>
    /// <remarks>
    /// "Blank" logos are 'included' but not 'shown'
    /// </remarks>
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

        // Masked entries are included
        if (variant.Mask is not null && tags.Intersect(variant.Mask.Tags).Any())
            return true;

        // Otherwise, Entries with tags are excluded by default
        return false;
    }

    private bool BoxIncludedInVariant(Box box)
    {
        if (variant.Pages.Count == 0 && box.Page == 0)
            return true;

        return variant.Pages.Contains(box.Page);
    }

    /// <summary>
    /// True if we should actually display the logo
    /// </summary>
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

        // Masked tags are shown at this stage, will be masked later
        if (variant.Mask is not null && logo.Tags.Intersect(variant.Mask.Tags).Any())
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