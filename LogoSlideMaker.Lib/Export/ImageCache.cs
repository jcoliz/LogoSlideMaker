using LogoSlideMaker.Primitives;
using SkiaSharp;
using Svg;
using System.Reflection;

namespace LogoSlideMaker.Export;

/// <summary>
/// Contains ready-to-draw image data for images we may want to add to PowerPoint
/// slides.
/// </summary>
public class ImageCache : IGetImageAspectRatio
{
    /// <summary>
    /// Base directory where files are located, or null for embedded storage
    /// </summary>
    public string? BaseDirectory
    {
        get => _BaseDirectory;

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
            if (!imageBuffers.ContainsKey(path))
            {
                var buffer = await LoadImageAsync(path);
                imageBuffers[path] = buffer;
                imageAspectRatios[path] = MeasureImage(isSvg: Path.GetExtension(path).ToLowerInvariant() == ".svg", buffer);
            }
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Unable to load image {path}", ex);
        }
    }

    /// <inheritdoc/>
    public bool Contains(string imagePath)
    {
        return imageAspectRatios.ContainsKey(imagePath);
    }

    /// <inheritdoc/>
    public decimal GetAspectRatio(string imagePath)
    {
        if (!imageAspectRatios.TryGetValue(imagePath, out var result))
        {
            throw new KeyNotFoundException($"No aspect ratio available for {imagePath}");
        }
        return result;
    }

    /// <summary>
    /// Get the buffer associated with this path, or null if we don't have it
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns></returns>
    public byte[]? GetOrDefault(string imagePath)
    {
        return imageBuffers.GetValueOrDefault(imagePath);
    }

    private readonly Dictionary<string, byte[]> imageBuffers = [];
    private readonly Dictionary<string, decimal> imageAspectRatios = [];

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
            var names = Assembly.GetEntryAssembly()!.GetManifestResourceNames();
            var resource = names.Where(x => x.Contains($".{filename}")).Single();
            stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(resource);
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

    private static decimal MeasureImage(bool isSvg, byte[] buffer)
    {
        using var stream = new MemoryStream(buffer);
        if (isSvg)
        {
            var svg = SvgDocument.Open<SvgDocument>(stream);
            var width = svg.Width.Value;
            var height = svg.Height.Value;
            if (svg.Width.Type == SvgUnitType.Percentage)
            {
                width *= svg.ViewBox.Width;
                height *= svg.ViewBox.Height;
            }
            return (decimal)(width/height);
        }
        else
        {
            var bitmap = SKBitmap.Decode(stream);
            return (decimal)bitmap.Width / bitmap.Height;
        }
    }
}
