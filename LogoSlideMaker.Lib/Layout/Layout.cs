using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Layout;

/// <summary>
/// Creates slide layouts for a specified slide definition (i.e. variant)
/// </summary>
public class LayoutEngine(Definition definition, Variant variant)
{
    public SlideLayout CreateSlideLayout()
    {
        // Find the boxes we're including
        var boxes = definition.Boxes.Where(x => BoxIncludedInVariant(x));

        // Add well-defined boxes
        var logos = boxes.Aggregate<Box, List<LogoLayout>>(new(), LayoutAggregateBox);

        // Add loose rows, only if pages are not specified
        if (variant.Pages.Count == 0)
        {
            logos.AddRange
            (
                definition.Rows.SelectMany(x => LayoutRow(x))
            );
        }

        // Add box titles
        // Note that we always lay them out if they're in the box definition.
        // It's up to the renderer if we're displaying them or not

        var text = Enumerable.Empty<TextLayout>();

        // TODO: Only supported for boxes with explicitly specified outer dimensions
        text = boxes
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && x.Outer != null)
            .Select(x => new TextLayout()
            {
                Text = x.Title,
                TextSyle = TextSyle.BoxTitle,
                // Titles have their bottom edge aligned with the top edge of the box
                Position = new Edge()
                {
                    X = x.Outer?.X ?? 0,
                    Y = x.Outer?.Y ?? 0,
                    Length = x.Outer?.Width ?? 0,
                    Kind = EdgeKind.Bottom
                }
            });

        return new SlideLayout() { Variant = variant, Logos = logos.ToArray(), Text = text.ToArray() };
    }

    /// <summary>
    /// Create a textual representation of this slide
    /// </summary>
    /// <returns>Lines of markdown</returns>
    public IEnumerable<string> AsMarkdown()
    {
        // Collect boxes and rows into boxlayouts

        // Add well-defined boxes
        var boxes =
            definition.Boxes
                .Where(x => BoxIncludedInVariant(x))
                .Select(x => (box: x, row: new Row() { Logos = x.Logos.SelectMany(y => y.Value).ToList() }))
                .Select(x => new BoxLayout() { Heading = x.box.Title, Logos = LayoutRow(x.row).ToArray() })
                .ToList();

        // Add loose rows into an "Others" box, only if pages are not specified
        if (variant.Pages.Count == 0 && definition.Rows.Count > 0)
        {
            boxes.Add
            (
                new BoxLayout()
                {
                    Heading = "Others",
                    Logos = LayoutRow(new Row() { Logos = definition.Rows.SelectMany(x => x.Logos).ToList() }).ToArray()
                }
            );
        }

        // Render them into text

        var result = new List<string>([string.Empty, $"## {variant.Name}"]);
        result.AddRange(variant.Description);
        result.AddRange(boxes.SelectMany(x =>
        {
            var result = new List<string>([string.Empty, $"### {x.Heading}", string.Empty]);
            result.AddRange
            (
                x.Logos
                    .Where(y => y.Logo is not null)
                    .Select(y => y.Logo!)
                    .Select(y =>
                    {
                        string alt_text = string.IsNullOrWhiteSpace(y.AltText) ? string.Empty : $"{y.AltText} ";
                        return $"* {alt_text}{y.Title}";
                    })
            );
            return result;
        }));

        return result;
    }

    /// <summary>
    /// Layout a single box, considering the state of already-laid-out boxes
    /// </summary>
    private List<LogoLayout> LayoutAggregateBox(List<LogoLayout> layouts, Box box)
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
            YPosition = last.Y + definition.Layout.LineSpacing + definition.Layout.BoxSpacing;
        }

        layouts.AddRange( LayoutBox(box, YPosition) );
        return layouts;
    }

    /// <summary>
    /// Layout a single box
    /// </summary>
    /// <param name="YPosition">
    /// Default yposition to use if box has no intrinsic position defined
    /// </param>
    private IEnumerable<LogoLayout> LayoutBox(Box box, decimal YPosition)
    {
        var logos = box.Logos
            .OrderBy(x => x.Key)
            .Select(x => x.Value);

        var flow = box.AutoFlow ? AutoFlow(box,logos) : logos;

        var layouts = flow
            .Select(MakeRow(box,YPosition))
            .SelectMany(x => LayoutRow(x));

        return layouts;
    }

    /// <summary>
    /// Reflow logos into matching-length rows
    /// </summary>
    private IEnumerable<List<string>> AutoFlow(Box box, IEnumerable<List<string>> logos)
    {
        // In order to autoflow, we first have to filter out unincluded logos.
        // This is a duplication of later logic, so should figure out how to NOT 
        // repeat that.

        var entries = IncludedEntries(logos.SelectMany(x => x));

        // Initially, we have this many rows
        var num_rows = box.NumRows ?? logos.Count();

        // We have this many logos
        var num_logos = entries.Count;

        // Ergo, we need this many columns
        var num_cols = num_logos / num_rows + (num_logos % num_rows > 0 ? 1 : 0);

        // However, if this is LESS than min-columns, use that instead
        if (num_cols < box.MinColumns)
        {
            num_cols = box.MinColumns;
        }

        var result = new List<List<string>>();
        var current = new List<string>();
        var current_col = 0;
        // Reflow the logos into the correct columns
        foreach(var logo in entries)
        {
            current.Add(logo.Id ?? string.Empty);
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
        var paddingX = (definition.Layout.PaddingX ?? definition.Layout.Padding ?? 0) + (box.MorePaddingX ?? 0);
        var paddingY = (definition.Layout.PaddingY ?? definition.Layout.Padding ?? 0);
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
                X = box.Outer.X + paddingX + definition.Render.TextWidth / 2m,
                Y = (box.Outer.Y is not null) ? box.Outer.Y + paddingY + definition.Render.IconSize / 2m : YPosition,
                Width = box.Outer.Width - paddingX * 2 - definition.Render.TextWidth
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
        var entries = IncludedEntries(_row.Logos);
        var row = _row with { Logos = entries.Select(x=>x.Id ?? string.Empty).ToList()};

        var layout = entries
            .TakeWhile(x=>x.Command != Commands.End)
            .Select((x,i)=>(logo:LookupLogo(x.Id),column:i))
            .Where(x=> LogoShownInVariant(x.logo))
            .Select(x=>new LogoLayout() { Logo = x.logo, X = row.XPosition + x.column * row.Spacing , Y = row.YPosition, DefaultTextWidth = variant.TextWidth });

        // If row ends up being empty, we still need to placehold vertical space for it
        return layout.Any() ? layout : [ new() { Y = row.YPosition } ];
    }

    /// <summary>
    /// Transform to entries, and filter out non-included entries
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    private ICollection<Entry> IncludedEntries(IEnumerable<string> logos)
    {
        return logos
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
            var logo = LookupLogo(entry.Id);

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
            var logo = LookupLogo(entry.Id);

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

    /// <summary>
    /// Find or create the logo matching this <paramref name="Id"/>
    /// </summary>
    /// <remarks>
    /// Looks up the logo in the defintion. If not found, will create a "missing" placeholder
    /// </remarks>
    /// <param name="Id">Id of logo</param>
    /// <returns>Logo created or found</returns>
    private Logo LookupLogo(string? Id)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            return new Logo() { Title = "Empty" };        
        }
        if (!definition.Logos.TryGetValue(Id, out var logo))
        {
            logo = new Logo() { Title = $"Missing Logo: {Id}" };
            definition.Logos[Id] = logo;
        }
        return logo;
    }
}

/// <summary>
/// A single logo with positioning
/// </summary>
public record LogoLayout
{
    public Logo? Logo { get; init; }
    /// <summary>
    /// Horizontal origin of where the logo should be placed, in inches
    /// </summary>
    /// <remarks>
    /// Logos currently use center as origin. This could change in the future/
    /// </remarks>
    public decimal X { get; init; }
    /// <summary>
    /// Vertical origin of where the logo should be placed, in inches
    /// </summary>
    public decimal Y { get; init; }

    /// <summary>
    /// Based on the layout, what is a good text width?
    /// </summary>
    /// <remarks>
    /// This can be overridden by the logo's own defined text width.
    /// Having it in layout allows it to vary by how many columns
    /// are in the box.
    /// </remarks>
    public decimal? DefaultTextWidth { get; init; }
}

/// <summary>
/// A group of logo layouts with a heading
/// </summary>
/// <remarks>
/// TODO: The heading is only used for text rendering. Text rendering should
/// be broken out into its own pipeline, and NOT done as part of the slide
/// rendering pipeline
/// </remarks>
public record BoxLayout
{
    public string? Heading { get; init; }
    public LogoLayout[] Logos { get; init; } = [];
}

/// <summary>
/// Additional text to display
/// </summary>
public record TextLayout
{
    /// <summary>
    /// The text to display. A value is expected
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Location and alignment the text box in inches
    /// </summary>
    public Edge Position { get; init; } = new();

    /// <summary>
    /// Visual style how the text should be rendered
    /// </summary>
    public TextSyle TextSyle { get; init; } = TextSyle.Invisible;
}

/// <summary>
/// All the logos laid out on a given slide, plus details about the variant
/// </summary>
/// <remarks>
/// TODO: Much of this is needed for text rendering, which should be separated
/// </remarks>
public record SlideLayout
{
    public Variant Variant { get; init; } = new();
    public LogoLayout[] Logos { get; init; } = [];
    public TextLayout[] Text { get; init; } = [];
}