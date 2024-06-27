﻿using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
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

        /// <summary>
        /// Of the slides (variants) defined in the definition, which one are we showing
        /// First slide is zero (TODO: Should be '1')
        /// </summary>
        public int SlideNumber 
        { 
            get
            {
                return _slideNumber;
            }
            set
            {
                if (_definition is not null && value < _definition.Variants.Count && value != _slideNumber)
                {
                    _slideNumber = value;

                    PopulateLayout();
                    GeneratePrimitives();
                }
            }
        }
        private int _slideNumber = 0;

        /// <summary>
        /// Display names of the slides
        /// </summary>
        public IEnumerable<string> SlideNames
        {
            get
            {
                if (_definition is null)
                {
                    return ["N/A"];
                }
                if (_definition.Variants.Count == 0)
                {
                    return ["Default"];
                }
                return _definition.Variants.Select((x,i) => $"{i}. {x.Name}");
            }
        }

        public void LoadDefinition(Stream stream)
        {
            var sr = new StreamReader(stream);
            var toml = sr.ReadToEnd();
            _definition = Toml.ToModel<Definition>(toml);

            PopulateLayout();

            GeneratePrimitives();
        }

        private void PopulateLayout()
        {
            if (_definition is null)
            {
                return;
            }

            var variant = _definition.Variants.Count > 0 ? _definition.Variants[_slideNumber] : new Variant();

            var engine = new LayoutEngine(_definition, variant);
            _layout = engine.CreateSlideLayout();
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
            if (_definition is null || _layout is null)
            {
                return;
            }

            _primitives.Clear();
            var config = _definition.Render;

            // Add primitives for a background
            var bgRect = new Configure.Rectangle() { X = 0, Y = 0, Width = 1280, Height = 720 };

            // If there is a bitmap template, draw that
            var definedBitmaps = _definition.Files.Template.Bitmaps;
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
            _primitives.AddRange(_layout.Logos.SelectMany(generator.ToPrimitives));

            // Add bounding boxes for any boxes with explicit outer dimensions
            _primitives.AddRange(
                _definition.Boxes
                    .Where(x => x.Outer is not null)
                    .Select(x => new RectanglePrimitive()
                    {
                        Rectangle = x.Outer! with 
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
        private SlideLayout? _layout;
        private readonly List<Primitive> _primitives = new();
    }
}
