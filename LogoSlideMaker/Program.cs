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
    definitions.Render.Listing = true;
}

if (definitions.Render.Listing)
{
    Console.WriteLine($"# {definitions.Layout.Title}");
}

int i = 0;
foreach(var variant in definitions.Variants)
{
    var copyingSlide = pres.Slides[variant.Source];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();

    var layout = new Layout(definitions, variant);
    layout.PopulateFrom();
    layout.RenderTo(slide.Shapes);

    ++i;
}

pres.SaveAs(options.Output!);

return 0;