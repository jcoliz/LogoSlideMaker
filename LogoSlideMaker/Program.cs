using ShapeCrawler;
using Tomlyn;

var pres = new Presentation("template.pptx");

var sr = new StreamReader("logos.toml");
var toml = sr.ReadToEnd();
var definitions = Toml.ToModel<Definition>(toml);


foreach(var variant in definitions.Variants)
{
    var copyingSlide = pres.Slides[0];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();
    var shapes = slide.Shapes;
    var renderer = new Renderer(definitions.Config, definitions.Logos, variant, shapes);

    foreach(var row in definitions.Rows)
    {
        renderer.Render(row);
    }
}


pres.SaveAs("out/1.pptx");
