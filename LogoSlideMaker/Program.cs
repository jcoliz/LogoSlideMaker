using ShapeCrawler;

var pres = new Presentation();
var slide = pres.Slides[0];
var shapes = slide.Shapes;

const double dpi = 96.0;

var x = 1.2;
var y = 1.12;
var width = 5.0;
var x_period_7 = width / 6.0;
var x_period_8 = width / 7.0;
var y_period = 1;
var icon_size = 0.25;

for(int i = 0; i < 7; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_7 - icon_size / 2)*dpi), y:(int)((y - icon_size / 2)*dpi), width:(int)(icon_size*dpi), height:(int)(icon_size*dpi));
}

for(int i = 0; i < 8; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_8 - icon_size / 2)*dpi), y:(int)((y + y_period - icon_size / 2)*dpi), width:(int)(icon_size*dpi), height:(int)(icon_size*dpi));
}


pres.SaveAs("out/1.pptx");