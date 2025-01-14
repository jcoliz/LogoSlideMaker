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

var template = options.Template ?? definition.TemplateSlidesFileName;
var listing = options.Listing || definition.Listing;
var output = options.Output ?? definition.OutputFileName;

if (string.IsNullOrWhiteSpace(output))
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: Must specify output file");
    return -1;
}

//
// LOAD IMAGES
//

var imageCache = new ImageCache() { BaseDirectory = basePath };
await imageCache.LoadAsync(definition.ImagePaths);

//
// EXPORT
//

using var templateStream = template is not null ? File.OpenRead(template) : null;
var export = new ExportPipelineEx(definition, imageCache);
export.Export(templateStream, output, options.Version);

//
// LISTING
//

if (listing)
{
    definition.RenderListing(Console.Out);
}

return 0;