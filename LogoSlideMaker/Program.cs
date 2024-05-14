using ShapeCrawler;
using Tomlyn;

var pres = new Presentation("template.pptx");

var sr = new StreamReader("logos.toml");
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

    foreach(var row in definitions.Rows)
    {
        renderer.Render(row);
    }

    ++i;
}


pres.SaveAs("out/1.pptx");
