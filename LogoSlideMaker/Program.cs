using ShapeCrawler;
using Tomlyn;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Render;
using LogoSlideMaker.Cli.Services;
using LogoSlideMaker.Primitives;
using static System.Runtime.InteropServices.JavaScript.JSType;

//
// OPTIONS
//

var options = new AppOptions();
options.Parse(args);

if (options.Exit)
{
    return -1;
}

//
// CONFIGURE
//

var sr = new StreamReader(options.Input!);
var toml = sr.ReadToEnd();
var definitions = Toml.ToModel<Definition>(toml);

//
// LOAD IMAGES
//

var images = new ImageCache() { BaseDirectory = Path.GetDirectoryName(options.Input) };
await images.LoadAsync(definitions.Logos.Select(x => x.Value.Path));

//
// LAYOUT
//

// Compose each slide
var slides = definitions.Variants.Select(x=> {
    var engine = new LayoutEngine(definitions, x);
    var layout = engine.CreateSlideLayout();
    return layout;
});

//
// RENDER
//

if (options.Listing)
{
    definitions.Render.Listing = true;
}

if (options.Output is not null)
{
    definitions.Files.Output = options.Output;
}

if (string.IsNullOrWhiteSpace(definitions.Files.Output))
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: Must specify output file");
    return -1;
}


// Open template or create new presentation
var pres = !string.IsNullOrWhiteSpace(options.Template) ? new Presentation(options.Template) : new Presentation();

// Renderer we will use for the slides
var renderer = new Renderer(definitions.Render);

// Render each slide
foreach(var layout in slides)
{
    //
    // PRIMITIVES
    //

    var gp = new GeneratePrimitives(definitions.Render, images);
    var primitives = layout.Logos.SelectMany(gp.ToPrimitives);

    var copyingSlide = pres.Slides[layout.Variant.Source];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();

    List<string> notes = [ $"Updated: {DateTime.Now:M/dd/yyyy h:mm tt K}"];
    if (options.Version is not null)
    {
        notes.Add($"Version: {options.Version}");
    }    
    notes.Add($"Logo count: {layout.Logos.Count(y=>y.Logo != null)}");
    slide.AddNotes(notes);

    //
    // RENDER TO PPTX
    //

    renderer.Render(primitives, slide.Shapes);
}

pres.SaveAs(definitions.Files.Output);

//
// LISTING
//

if (definitions.Render.Listing)
{
    var markdown = new List<string>([$"# {definitions.Layout.Title}"]);

    markdown.AddRange(definitions.Variants.SelectMany(x => new LayoutEngine(definitions, x).AsMarkdown()));

    foreach(var line in markdown)
    {
        Console.WriteLine(line);
    }
}

return 0;