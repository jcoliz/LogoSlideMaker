﻿using ShapeCrawler;
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
// LAYOUT
//

// Compose each layout
var layouts = definitions.Variants.Select(x=> {
    var layout = new Layout(definitions, x);
    layout.Populate();
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
    Console.WriteLine($"# {definitions.Layout.Title}");
}

// Open template or create new presentation
var pres = !string.IsNullOrWhiteSpace(options.Template) ? new Presentation(options.Template) : new Presentation();

// Renderer we will use for the slides
var renderer = new Renderer(definitions.Render);

// Render each layout
foreach(var layout in layouts)
{
    var copyingSlide = pres.Slides[layout.Source];
    pres.Slides.Add(copyingSlide);
    var slide = pres.Slides.Last();

    slide.AddNotes([ $"*** Updated: {DateTime.Now:M/dd/yyyy h:mm tt K}"]);

    renderer.Render(layout, slide.Shapes);
}

pres.SaveAs(options.Output!);

return 0;