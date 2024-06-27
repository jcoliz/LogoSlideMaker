using LogoSlideMaker.Primitives;
using Microsoft.Graphics.Canvas;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogoSlideMaker.WinUi.Services
{
    /// <summary>
    /// Contains ready-to-draw canvas bitmaps for all images we may want to draw
    /// </summary>
    internal class BitmapCache : IGetImageSize
    {
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
            // We can only load PNGs right now
            if (!bitmaps.ContainsKey(path))
            {
                var cb = await LoadBitmapAsync(resourceCreator, path);
                bitmaps[path] = cb;

                var bounds = cb.GetBounds(resourceCreator);
                bitmapSizes[path] = new Configure.Size() { Width = (decimal)bounds.Width, Height = (decimal)bounds.Height };
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

        public CanvasBitmap? GetOrDefault(string imagePath)
        {
            return bitmaps.GetValueOrDefault(imagePath);
        }

        private readonly Dictionary<string, CanvasBitmap> bitmaps = new();
        private readonly Dictionary<string, Configure.Size> bitmapSizes = new();

        /// <summary>
        /// Load a single bitmap from embedded storage
        /// </summary>
        /// <param name="resourceCreator">Where to create bitmaps</param>
        /// <param name="filename">Name of source file</param>
        /// <returns>Created bitmap in this canvas</returns>
        private async Task<CanvasBitmap> LoadBitmapAsync(ICanvasResourceCreator resourceCreator, string filename)
        {
            // TODO: Also need to be able to load bitmaps from local storage!!

            var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
            var resource = names.Where(x => x.Contains($".{filename}")).Single();
            var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

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
}
