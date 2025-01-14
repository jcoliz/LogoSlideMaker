using LogoSlideMaker.Cli;
using LogoSlideMaker.Export;
using LogoSlideMaker.Public;

//
// OPTIONS
//

var options = new AppOptions();
options.Parse(args);

if (options.Exit)
{
    return -1;
}

var basePath = Path.GetDirectoryName(options.Input);

//
// CONFIGURE
//

using var stream = File.OpenRead(options.Input!);
var definition = Loader.Load(stream, basePath);
definition.OverrideWithOptions(options.Template, options.Listing, options.Output);

if (string.IsNullOrWhiteSpace(definition.OutputFileName))
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: Must specify output file");
    return -1;
}

//
// LOAD IMAGES
//

var exportPipeline = new ExportPipelineEx(definition);
await exportPipeline.LoadAndMeasureAsync(Path.GetDirectoryName(options.Input)!);

//
// EXPORT
//

using var templateStream = definition.TemplateSlidesFileName is not null ? File.OpenRead(definition.TemplateSlidesFileName) : null;
exportPipeline.Save(templateStream, definition.OutputFileName, options.Version);

//
// LISTING
//

if (definition.Listing)
{
    definition.RenderListing(Console.Out);
}

return 0;