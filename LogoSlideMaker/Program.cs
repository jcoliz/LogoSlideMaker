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

var logos = new Dictionary<string,Logo>()
{
    { "blender", new Logo() { Title = "Hello, Blender", Path = "blender_icon_128x128.png" } }
};

Row[] rows =
[
    new Row() 
    { 
        Width = 4.85, 
        XPosition = 1.2, 
        YPosition = 1.12,
        Logos = [ 
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
        ]
    },
    new Row() 
    { 
        Width = 4.85, 
        XPosition = 1.2, 
        YPosition = 1.12 + 0.6,
        Logos = [ 
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
            "blender",
        ]
    }
];

var renderer = new Renderer(config, logos, shapes);
foreach(var row in rows)
{
    renderer.Render(row);
}

pres.SaveAs("out/1.pptx");
