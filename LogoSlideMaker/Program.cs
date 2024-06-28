using ShapeCrawler;
using Tomlyn;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Render;

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

// TODO

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

if (definitions.Render.Listing)
{
    // TODO: In the case of "listing", render the listing as a separate pass.
    Console.WriteLine($"# {definitions.Layout.Title}");
}

// Open template or create new presentation
var pres = !string.IsNullOrWhiteSpace(options.Template) ? new Presentation(options.Template) : new Presentation();

// Renderer we will use for the slides
var renderer = new Renderer(definitions.Render);

// Render each slide
foreach(var layout in slides)
{
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
    // Generate primitives
    //

    // TODO

    //
    // Render slide
    //

    // TODO: Don't send the layout, just send the primitives, plus whatevrer
    // else the slide renderer needs for metadata

    renderer.Render(layout, slide.Shapes);
}

pres.SaveAs(options.Output!);

return 0;