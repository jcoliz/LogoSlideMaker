using ShapeCrawler;
using Tomlyn;

var pres = new Presentation();
var slide = pres.Slides[0];
var shapes = slide.Shapes;

var sr = new StreamReader("logos.toml");
var toml = sr.ReadToEnd();
var definitions = Toml.ToModel<Definition>(toml);

var renderer = new Renderer(definitions.Config, definitions.Logos, shapes);
foreach(var row in definitions.Rows)
{
    renderer.Render(row);
}

pres.SaveAs("out/1.pptx");
