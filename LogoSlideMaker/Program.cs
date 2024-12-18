﻿using LogoSlideMaker.Cli;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Export;
using Tomlyn;

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
definitions.ProcessAfterLoading();

if (!string.IsNullOrWhiteSpace(options.Template))
{
    definitions.Files.Template.Slides = options.Template;
}

if (options.Listing)
{
    definitions.Render.Listing = true;
}

if (!string.IsNullOrWhiteSpace(options.Output))
{
    definitions.Files.Output = options.Output;
}

if (string.IsNullOrWhiteSpace(definitions.Files.Output))
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: Must specify output file");
    return -1;
}

if (!string.IsNullOrWhiteSpace(definitions.Files.Include.Logos))
{
    var dir = Path.GetDirectoryName(options.Input);
    var logopath = Path.Combine(dir!, definitions.Files.Include.Logos);

    using var logostream = File.OpenRead(logopath);
    using var logosr = new StreamReader(logostream);
    var logotoml = logosr.ReadToEnd();
    var logos = Toml.ToModel<Definition>(logotoml);

    definitions.IncludeLogosFrom(logos);
}

//
// LOAD IMAGES
//

var exportPipeline = new ExportPipeline(definitions);
await exportPipeline.LoadAndMeasureAsync(Path.GetDirectoryName(options.Input)!);

//
// EXPORT
//

using var templateStream = definitions.Files.Template.Slides is not null ? File.OpenRead(definitions.Files.Template.Slides) : null;
exportPipeline.Save(templateStream, definitions.Files.Output, options.Version);

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