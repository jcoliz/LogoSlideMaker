using LogoSlideMaker.Configure;
using LogoSlideMaker.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tomlyn;

namespace LogoSlideMaker.WinUi.ViewModels
{
    internal class MainViewModel(IGetImageSize bitmaps)
    {
        public IReadOnlyList<Primitive> Primitives => _primitives;

        public IEnumerable<string> ImagePaths =>
            _definition?.Logos.Select(x => x.Value.Path).Concat(_definition.Files.Template.Bitmaps) ?? [];

        public RenderConfig? RenderConfig => _definition?.Render;

        public void LoadDefinition(Stream stream)
        {
            var sr = new StreamReader(stream);
            var toml = sr.ReadToEnd();
            _definition = Toml.ToModel<Definition>(toml);

            _layout = new Layout.Layout(_definition, new Variant());
            _layout.Populate();

            GeneratePrimitives();
        }

        /// <summary>
        /// Generate and retain all primitives needed to display this slide
        /// </summary>
        /// <remarks>
        /// Note that we can't generate primitives until we've loaded and (more importantly)
        /// measured all the images
        /// </remarks>
        public void GeneratePrimitives()
        {
            _primitives.Clear();
            var config = _definition.Render;

            // Add primitives for a background
            var bgRect = new Configure.Rectangle() { X = 0, Y = 0, Width = 1280, Height = 720 };

            // If there is a bitmap template, draw that
            var definedBitmaps = _definition?.Files.Template.Bitmaps;
            if (definedBitmaps is not null && definedBitmaps.Count > 0 && bitmaps.Contains(definedBitmaps[0]))
            {
                _primitives.Add(new ImagePrimitive()
                {
                    Rectangle = bgRect,
                    Path = definedBitmaps[0]
                });
            }
            else
            {
                // Else Draw a white background
                _primitives.Add(new RectanglePrimitive()
                {
                    Rectangle = bgRect,
                    Fill = true
                });
            }

            // Add needed primitives for each logo
            var generator = new GeneratePrimitives(config, bitmaps);
            _primitives.AddRange(_layout.SelectMany(x => x.Logos).SelectMany(generator.ToPrimitives));

            // Add bounding boxes for any boxes with explicit outer dimensions
            _primitives.AddRange(
                _definition.Boxes
                    .Where(x => x.Outer is not null)
                    .Select(x => new RectanglePrimitive()
                    {
                        Rectangle = x.Outer with 
                        { 
                            X = x.Outer.X * 96m, 
                            Y = x.Outer.Y * 96m,
                            Width = x.Outer.Width * 96m,
                            Height = x.Outer.Height * 96m
                        }
                    }
                )
            );
        }

        private Definition? _definition;
        private Layout.Layout? _layout;
        private readonly List<Primitive> _primitives = new();
    }
}
