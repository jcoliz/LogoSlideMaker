using DocumentFormat.OpenXml.Linq;
using LogoSlideMaker.Primitives;
using SkiaSharp;
using Svg;
using System.Reflection;

namespace LogoSlideMaker.Export;

/// <summary>
/// Contains ready-to-draw canvas bitmaps for all images we may want to draw
/// </summary>
public class ImageCache : IGetImageSize
{
    /// <summary>
    /// Base directory where files are located, or null for embedded storage
    /// </summary>
    public string? BaseDirectory 
    {
        get
        {
            return _BaseDirectory;
        }

        set
        {
            if (value == string.Empty)
            {
                _BaseDirectory = ".";
            }
            else
            {                
                _BaseDirectory = value;
            }
        }
    }
    private string? _BaseDirectory;

    /// <summary>
    /// Load and retain bitmap for each paths if not already present
    /// </summary>
    /// <param name="paths">Paths to resource files</param>
    public async Task LoadAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await LoadAsync(path);
        }
    }

    /// <summary>
    /// Load and retain stream for this path, if not already present
    /// </summary>
    /// <remarks>
    /// NOTE: We cache streams, not loaded image files, because the underlying ShapeCrawler
    /// library will only create an image from a stream, not a loaded image.
    /// </remarks>
    /// <param name="path">Path to resource file</param>
    public async Task LoadAsync(string path)
    {
        try
        {
            if (!bitmaps.ContainsKey(path))
            {
                var buffer = await LoadImageAsync(path);
                bitmaps[path] = buffer;
                bitmapSizes[path] = MeasureImage(isSvg: Path.GetExtension(path).ToLowerInvariant() == ".svg", buffer);
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Unable to load image {path}", ex);
        }
    }

    public bool Contains(string imagePath)
    {
        return bitmapSizes.ContainsKey(imagePath);
    }

    public Configure.Size GetSize(string imagePath)
    {
        return bitmapSizes.GetValueOrDefault(imagePath) ?? throw new KeyNotFoundException();
    }

    public byte[]? GetOrDefault(string imagePath)
    {
        return bitmaps.GetValueOrDefault(imagePath);
    }

    private readonly Dictionary<string, byte[]> bitmaps = new();
    private readonly Dictionary<string, Configure.Size> bitmapSizes = new();

    /// <summary>
    /// Load a single image from embedded or external storage as a memory stream
    /// </summary>
    /// <param name="filename">Name of source file</param>
    /// <returns>Memory stream for this file</returns>
    private async Task<byte[]> LoadImageAsync(string filename)
    {

        Stream? stream = null;
        if (BaseDirectory is null)
        {
            var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
            var resource = names.Where(x => x.Contains($".{filename}")).Single();
            stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        }
        else
        {
            var fullPath = Path.GetFullPath(BaseDirectory + Path.DirectorySeparatorChar + filename);
            stream = File.OpenRead(fullPath);
        }

        var result = new MemoryStream();
        await stream!.CopyToAsync(result);
        result.Position = 0;

        stream.Dispose();

        return result.GetBuffer();
    }

    private Configure.Size MeasureImage(bool isSvg, byte[] buffer)
    {
        using var stream = new MemoryStream(buffer);
        if (isSvg)
        {
            var svg = SvgDocument.Open<SvgDocument>(stream);
            return new() { Width = (decimal)svg.Width.Value, Height = (decimal)svg.Height.Value };
        }
        else
        {
            var bitmap = SKBitmap.Decode(stream);
            return new() { Width = bitmap.Width, Height = bitmap.Height };
        }
    }
}
