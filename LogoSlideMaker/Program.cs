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

for(int i = 0; i < 7; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_7)*dpi), y:(int)(y*dpi), width:(int)(0.22*dpi), height:(int)(0.22*dpi));
}

for(int i = 0; i < 8; i++)
{
    shapes.AddRectangle(x:(int)((x + i * x_period_8)*dpi), y:(int)((y + y_period)*dpi), width:(int)(0.22*dpi), height:(int)(0.22*dpi));
}


pres.SaveAs("out/1.pptx");