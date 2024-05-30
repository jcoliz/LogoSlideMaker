using ShapeCrawler;
using Tomlyn;

var options = new AppOptions();
options.Parse(args);

if (options.Exit)
{
    return -1;
}


var pres = !string.IsNullOrWhiteSpace(options.Template) ? new Presentation(options.Template) : new Presentation();

var sr = new StreamReader(options.Input!);
var toml = sr.ReadToEnd();
var definitions = Toml.ToModel<Definition>(toml);

if (options.Listing)
{
    definitions.Config.Listing = true;
}

if (definitions.Config.Listing)
{
    Console.WriteLine($"# {definitions.Config.Title}");
}

int i = 0;
foreach(var variant in definitions.Variants)
{
    if (definitions.Config.Listing)
    {   
        Console.WriteLine();
        Console.WriteLine($"## {variant.Name}");
        Console.WriteLine();
        Console.WriteLine(variant.Description);
    }

    var copyingSlide = pres.Slides[variant.Source];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();
    var shapes = slide.Shapes;

    // Fill in description field
    if (variant.Description.Count > 0)
    {
        var description_box = shapes.TryGetByName<IShape>("Description");
        if (description_box is not null)
        {
            var tf = description_box.TextFrame;
            var maxlines = Math.Min(variant.Description.Count,tf.Paragraphs.Count);
            for (int l = 0; l < maxlines; l++)
            {
                tf.Paragraphs[l].Text = variant.Description[l];
            }
        }
    }

    var renderer = new Renderer(definitions.Config, definitions.Logos, variant, shapes);

    foreach(var box in definitions.Boxes)
    {
        if (definitions.Config.Listing)
        {   
            Console.WriteLine();
            Console.WriteLine($"### {box.Title}");
            Console.WriteLine();
        }

        foreach (var row in box.GetRows(spacing:definitions.Config.LineSpacing, default_width:definitions.Config.DefaultWidth))
        {
            renderer.Render(row);
        }
    }

    foreach(var row in definitions.Rows)
    {
        if (definitions.Config.Listing)
        {   
            Console.WriteLine();
        }
        renderer.Render(row);
    }

    ++i;
}

pres.SaveAs(options.Output!);

return 0;