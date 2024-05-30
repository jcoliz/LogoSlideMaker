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