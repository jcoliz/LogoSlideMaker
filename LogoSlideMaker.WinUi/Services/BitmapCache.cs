using LogoSlideMaker.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Svg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LogoSlideMaker.WinUi.Services;

/// <summary>
/// Contains ready-to-draw canvas bitmaps for all images we may want to draw
/// </summary>
public class BitmapCache(ILogger<BitmapCache> logger) : IGetImageAspectRatio
{
    /// <summary>
    /// Base directory where files are located, or null for embedded storage
    /// </summary>
    public string? BaseDirectory { get; set; }

    /// <summary>
    /// Load and retain bitmap for each paths if not already present
    /// </summary>
    /// <param name="paths">Paths to resource files</param>
    public async Task LoadAsync(ICanvasResourceCreator resourceCreator, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await LoadAsync(resourceCreator, path);
        }
    }

    /// <summary>
    /// Load and retain bitmap for this path, if not already present
    /// </summary>
    /// <param name="path">Path to resource file</param>
    public async Task LoadAsync(ICanvasResourceCreator resourceCreator, string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && !bitmaps.ContainsKey(path))
            {
                var cb = await LoadBitmapAsync(resourceCreator, path);
                bitmaps[path] = cb;

                var bounds = cb.GetBounds(resourceCreator);
                bitmapAspectRatios[path] = (decimal)bounds.Width / (decimal)bounds.Height;
            }
        }
        catch (DirectoryNotFoundException)
        {
            // This is a user error that will be handled by displaying a blank logo
            logger.LogDebug("Cache LoadBitmap: Directory not found {Path}", path);
        }
        catch (FileNotFoundException)
        {
            // This is a user error that will be handled by displaying a blank logo
            logger.LogDebug("Cache LoadBitmap: File not found {Path}", path);
        }
        catch (Exception ex)
        {
            // We should investigate any of these to see how we want to handle other
            // kinds of errors.
            logger.LogError(ex, "Cache LoadBitmap: Failed to load {Path}", path);
        }
    }

    /// <inheritdoc/>
    public bool Contains(string imagePath)
    {
        return bitmapAspectRatios.ContainsKey(imagePath);
    }

    /// <inheritdoc/>
    public decimal GetAspectRatio(string imagePath)
    {
        if (!bitmapAspectRatios.TryGetValue(imagePath, out var result))
        {
            throw new KeyNotFoundException($"No aspect ratio available for {imagePath}");
        }
        return result;
    }

    /// <summary>
    /// Get the bitmap available, or default if we dont have one
    /// </summary>
    /// <param name="imagePath">Filesystem path to use as a key</param>
    /// <returns></returns>
    public CanvasBitmap? GetOrDefault(string imagePath)
    {
        return bitmaps.GetValueOrDefault(imagePath);
    }

    private readonly Dictionary<string, CanvasBitmap> bitmaps = [];
    private readonly Dictionary<string, decimal> bitmapAspectRatios = [];

    /// <summary>
    /// Load a single bitmap from embedded storage
    /// </summary>
    /// <param name="resourceCreator">Where to create bitmaps</param>
    /// <param name="filename">Name of source file</param>
    /// <returns>Created bitmap in this canvas</returns>
    private async Task<CanvasBitmap> LoadBitmapAsync(ICanvasResourceCreator resourceCreator, string filename)
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

        if (filename.ToLowerInvariant().EndsWith(".svg"))
        {
            var svg = SvgDocument.Open<SvgDocument>(stream);
            var bitmap = svg.Draw();
            var pngStream = new MemoryStream();
            bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
            pngStream.Seek(0, SeekOrigin.Begin);
            var randomAccessStream = pngStream.AsRandomAccessStream();
            var result = await CanvasBitmap.LoadAsync(resourceCreator, randomAccessStream);

            return result;
        }
        else
        {
            var randomAccessStream = stream.AsRandomAccessStream();
            var result = await CanvasBitmap.LoadAsync(resourceCreator, randomAccessStream);

            return result;
        }
    }
}
