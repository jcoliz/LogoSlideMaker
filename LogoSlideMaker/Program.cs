using DocumentFormat.OpenXml.Drawing;
using ShapeCrawler;

var pres = new Presentation();
var slide = pres.Slides[0];
var shapes = slide.Shapes;

var config = new Config() { Dpi = 96.0, IconSize = 0.21, TextDistace = 0.27, TextHeight = 0.24, TextWidth = 0.67 };

var row = new Row(config) { NumItems = 7, Width = 4.85, XPosition = 1.2, YPosition = 1.12 };
row.RenderTo(shapes);

row = new Row(config) { NumItems = 8, Width = 4.85, XPosition = 1.2, YPosition = 1.12 + 0.6 };
row.RenderTo(shapes);

pres.SaveAs("out/1.pptx");
