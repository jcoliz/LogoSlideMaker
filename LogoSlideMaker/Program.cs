using ShapeCrawler;
using Tomlyn;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Render;

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

var renderer = new Renderer(definitions.Render);

int i = 0;
foreach(var variant in definitions.Variants)
{
    var copyingSlide = pres.Slides[variant.Source];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();

    var layout = new Layout(definitions, variant);
    layout.Populate();
    renderer.Render(layout, slide.Shapes);

    ++i;
}

pres.SaveAs(options.Output!);

return 0;