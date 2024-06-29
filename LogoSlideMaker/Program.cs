using Tomlyn;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Export;

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

var exportPipeline = new ExportPipeline(definitions);
await exportPipeline.LoadAndMeasureAsync(Path.GetDirectoryName(options.Input)!);

//
// EXPORT
//

if (string.IsNullOrWhiteSpace(definitions.Files.Output))
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: Must specify output file");
    return -1;
}

exportPipeline.Save(options.Template, definitions.Files.Output, options.Version);

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