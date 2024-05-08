using DocumentFormat.OpenXml.Drawing;
using ShapeCrawler;

var pres = new Presentation();
var slide = pres.Slides[0];
var shapes = slide.Shapes;

const double dpi = 96.0;

var x = 1.2;
var y = 1.12;
var width = 4.85;
var x_period_7 = width / 6.0;
var x_period_8 = width / 7.0;
var y_period = 0.6;
var icon_size = 0.21;

int n = 1;

for(int i = 0; i < 7; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_7 - icon_size / 2)*dpi), y:(int)((y - icon_size / 2)*dpi), width:(int)(icon_size*dpi), height:(int)(icon_size*dpi));

    shapes.AddRectangle(x:(int)((x + i * x_period_7 - 0.67 / 2)*dpi), y:(int)((y - 0.24 / 2 + 0.27)*dpi), width:(int)(0.67*dpi), height:(int)(0.24*dpi));

    var shape = shapes.Last();
    var tf = shape.TextFrame;
    tf.Text = $"Hello, Icon #{n++}!";
    var font = tf.Paragraphs.First().Portions.First().Font;
    font.Size = 7;
    font.LatinName = "Segoe UI";
    font.Color.Update("595959");
    shape.Fill.SetColor("FFFFFF");
    shape.Outline.HexColor = "FFFFFF";
}

for(int i = 0; i < 8; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_8 - icon_size / 2)*dpi), y:(int)((y + y_period - icon_size / 2)*dpi), width:(int)(icon_size*dpi), height:(int)(icon_size*dpi));

    shapes.AddRectangle(x:(int)((x + i * x_period_8 - 0.67 / 2)*dpi), y:(int)((y + y_period - 0.24 / 2 + 0.27)*dpi), width:(int)(0.67*dpi), height:(int)(0.24*dpi));

    var shape = shapes.Last();
    var tf = shape.TextFrame;
    tf.Text = $"Hello, Icon #{n++}!";
    var font = tf.Paragraphs.First().Portions.First().Font;
    font.Size = 7;
    font.LatinName = "Segoe UI";
    font.Color.Update("595959");
    shape.Fill.SetColor("FFFFFF");
    shape.Outline.HexColor = "FFFFFF";

}


pres.SaveAs("out/1.pptx");