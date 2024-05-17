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

int i = 0;
foreach(var variant in definitions.Variants)
{
    var copyingSlide = pres.Slides.Last();
    pres.Slides.Insert(i + 1, copyingSlide);
    var slide = pres.Slides[i];
    var shapes = slide.Shapes;
    var renderer = new Renderer(definitions.Config, definitions.Logos, variant, shapes);

    foreach(var row in definitions.AllRows)
    {
        renderer.Render(row);
    }

    ++i;
}

pres.SaveAs(options.Output!);

return 0;