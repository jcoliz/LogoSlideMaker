using ShapeCrawler;

var pres = new Presentation();
var slide = pres.Slides[0];
var shapes = slide.Shapes;

var config = new Config() 
{ 
    Dpi = 96.0, 
    IconSize = 0.21, 
    TextDistace = 0.27, 
    TextHeight = 0.24, 
    TextWidth = 0.67 
};

Row[] rows =
[
    new Row() 
    { 
        Width = 4.85, 
        XPosition = 1.2, 
        YPosition = 1.12,
        Logos = [ 
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
        ]
    },
    new Row() 
    { 
        Width = 4.85, 
        XPosition = 1.2, 
        YPosition = 1.12 + 0.6,
        Logos = [ 
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
            "blender_icon_128x128.png",
        ]
    }
];

var renderer = new Renderer(config,shapes);
foreach(var row in rows)
{
    renderer.Render(row);
}

pres.SaveAs("out/1.pptx");
