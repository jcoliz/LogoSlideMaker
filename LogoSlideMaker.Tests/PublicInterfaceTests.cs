﻿using DocumentFormat.OpenXml.Linq;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using LogoSlideMaker.Tests.Helpers;
using ShapeCrawler.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;

namespace LogoSlideMaker.Tests
{
    internal class PublicInterfaceTests: TestsBase
    {
        /// <summary>
        /// Scenario: Loader loads file with variant
        /// </summary>
        [Test]
        public void LoadsOk()
        {
            // When: Loading a definition with one variant
            var definition = Loader.Load(GetStream("variant.toml"));

            // Then: Result has one variant
            Assert.That(definition.Variants, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Scenario: Loader loads file with no title
        /// </summary>
        [Test]
        public void LoadsOkNoTitle()
        {
            // When: Loading a definition with no title
            var definition = Loader.Load(GetStream("simple.toml"));

            // Then: Title is null
            Assert.That(definition.Variants[0].DocumentTitle, Is.Null);
        }

        /// <summary>
        /// Scenario: Loader loads file with title
        /// </summary>
        [Test]
        public void LoadsOkTitle()
        {
            // When: Loading a definition with a title
            var definition = Loader.Load(GetStream("title.toml"));

            // Then: Title is as expected
            Assert.That(definition.Variants[0].DocumentTitle, Is.EqualTo("Document Title"));
        }

        /// <summary>
        /// Scenario: Loader loads file with no variant
        /// </summary>
        [Test]
        public void LoadsOkNoVariant()
        {
            // When: Loading a definition with no variants
            var definition = Loader.Load(GetStream("simple.toml"));

            // Then: Result has one variant (Because we created one)
            Assert.That(definition.Variants, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Scenario: Loader loads variant name and descriptions
        /// </summary>
        [Test]
        public void VariantProperties()
        {
            // When: Loading a definition with one variant
            var definition = Loader.Load(GetStream("variant.toml"));

            // Then: Variant name is as expected
            Assert.That(definition.Variants.First().Name, Is.EqualTo("Test Variant"));

            // And: Variant description is as expected
            Assert.That(definition.Variants.First().Description, Is.EquivalentTo(["one","two"]));
        }

        [Test]
        public void VariantGeneratePrimitives()
        {
            // Given: A definition with one variant using a template with one slide
            var definition = Loader.Load(GetStream("simple.toml"));

            // When: Generating the primitives
            var primitives = definition.Variants.First().GeneratePrimitives(new TestImageSource());

            // Then: Has 5 primitives
            Assert.That(primitives, Has.Count.EqualTo(5));

            // Then: Has 1 BG primitive
            Assert.That(primitives.Where(x=>x.Purpose==Primitives.PrimitivePurpose.Background).Count(), Is.EqualTo(1));

            // Then: Has 2 Image Primitives
            Assert.That(primitives.Where(x => x.GetType() == typeof(ImagePrimitive)).Count(), Is.EqualTo(2));

            // Then: Has 2 Text Primitives
            Assert.That(primitives.Where(x => x.GetType() == typeof(TextPrimitive)).Count(), Is.EqualTo(2));
        }

        [Test]
        public void VariantGenerateEmptyPrimitives()
        {
            // Given: A definition with two logos, one of wihch has no image path specified
            var definition = Loader.Load(GetStream("image-paths-empty.toml"));

            // When: Generating the primitives
            var primitives = definition.Variants.First().GeneratePrimitives(new TestImageSource());

            // Then: Has 1 Image Primitives
            Assert.That(primitives.Where(x => x.GetType() == typeof(ImagePrimitive)).Count(), Is.EqualTo(1));

            // And: Has 1 Base Rectangle Primitive
            Assert.That(
                primitives
                .Where(x => x.GetType() == typeof(RectanglePrimitive) && x.Purpose == PrimitivePurpose.Base)
                .Count(), 
                Is.EqualTo(1)
            );

            // And: Has 2 Text Primitives
            Assert.That(primitives.Where(x => x.GetType() == typeof(TextPrimitive)).Count(), Is.EqualTo(2));
        }
        [Test]
        public void ImagePaths()
        {
            // Given: A definition with logo and template image paths
            var definition = Loader.Load(GetStream("image-paths.toml"));

            // When: Getting image paths
            var paths = definition.ImagePaths;

            // Then: Has 4 image paths
            Assert.That(paths, Has.Count.EqualTo(4));
        }

        [Test]
        public void ImageCropLoads()
        {
            // Given: A logo with excess imagery we don't want
            // And: Specifying 'crop` dimensions in the definition
            var definition = Loader.Load(GetStream("crop.toml")) as PublicDefinition;

            // When: Checking loaded logo
            var logo = definition!.Definition.Logos["one"];

            // Then: Crop rectangle loaded as expected
            Assert.That(logo.Crop,Is.EqualTo(new Frame() { Right = 0.75m }));
        }

        [Test]
        public void ImageNoCropLoads()
        {
            // Given: A logo with excess imagery we don't want
            // And: Specifying 'crop` dimensions in the definition
            // When: Loading the definition
            var definition = Loader.Load(GetStream("crop.toml")) as PublicDefinition;

            // When: Checking a different  logo
            var logo = definition!.Definition.Logos["two"];

            // Then: Crop rectangle loaded as expected
            Assert.That(logo.Crop,Is.Null);
        }

        [TestCase("./one.png", "[50, 200]")]
        [TestCase("./left.png", "[50, 200]")]
        [TestCase("./left-right.png", "[70.71, 141.42]")]
        [TestCase("./top-bottom.png", "[141.42, 70.71]")]
        [TestCase("./all.png", "[100, 100]")]
        public void ImageCropCorrectSize(string filename, string dimensionsjson)
        {
            // Given: A logo with excess imagery we don't want
            // And: Specifying 'crop` dimensions in the definition
            var definition = Loader.Load(GetStream("crop.toml")) as PublicDefinition;

            // When: Generating primitives
            var primitives = definition!.Variants[0].GeneratePrimitives(new TestImageSource());

            // And: Examining the primitive for the cropped logo
            var primitive = primitives
                .Where(x => x is ImagePrimitive)
                .Cast<ImagePrimitive>().Where(x => x.Path == filename)
                .Single()!;

            // Then: Size of image is as expected
            var dimensions = JsonSerializer.Deserialize<decimal[]>(dimensionsjson)!;
            Assert.That(primitive.Rectangle.Width, Is.EqualTo(dimensions[0]).Within(0.01m));
            Assert.That(primitive.Rectangle.Height,Is.EqualTo(dimensions[1]).Within(0.01m));
        }

        [Test]
        public void LogoCornerLoads()
        {
            // Given: A definition with a logo having specified corner radius
            var definition = Loader.Load(GetStream("corner-radius.toml")) as PublicDefinition;

            // When: Checking loaded logo
            var logo = definition!.Definition.Logos["zero"];

            // Then: Crop rectangle loaded as expected
            Assert.That(logo.CornerRadius,Is.EqualTo(0.1m));
        }

        [Test]
        public void LogoCornerInPrimitives()
        {
            // Given: A definition with a logo having specified corner radius
            var definition = Loader.Load(GetStream("corner-radius.toml"));

            // When: Generating primitives
            var primitives = definition.Variants[0].GeneratePrimitives(new TestImageSource());

            // And: Examining the primitive for the logo
            var primitive = primitives
                .Where(x => x is ImagePrimitive)
                .Cast<ImagePrimitive>().Where(x => x.Path == "rect-4x1.svg")
                .First()!;

            // Then: Primitive has expected corner radius value
            // Expected 96 dpi * 1 inches * 0.1 radius ratio
            Assert.That(primitive.CornerRadius,Is.EqualTo(9.6m));
        }

        /// <summary>
        /// Scenario: Image Paths should not contain empty paths
        /// </summary>
        [Test]
        public void EmptyPathsNotInImagePaths()
        {
            // Given: A definition containing a logo with no path specified
            var definition = Loader.Load(GetStream("image-paths-empty.toml"));

            // When: Getting the image paths
            var paths = definition.ImagePaths;

            // Then: There are no empty paths
            Assert.That(paths, Has.None.EqualTo(string.Empty));
        }

        [Test]
        public void TextStyles()
        {
            // Given: A definition which defines text styles
            var definition = Loader.Load(GetStream("text-styles.toml"));

            // When: Getting the styles through the default variant
            var styles = definition.Variants[0].TextStyles;

            // Then: The values are as expected
            Assert.That(styles[Models.TextSyle.Logo].FontName,Is.EqualTo("Logo Font"));
            Assert.That(styles[Models.TextSyle.Logo].FontColor,Is.EqualTo("111111"));
            Assert.That(styles[Models.TextSyle.BoxTitle].FontSize,Is.EqualTo(24));
            Assert.That(styles[Models.TextSyle.BoxTitle].FontName,Is.EqualTo("Segoe UI"));
        }

        [Test]
        [Explicit("Failing test for new feature.")]
        public void TextStylesNew()
        {
            // Given: A definition which defines text styles
            var definition = Loader.Load(GetStream("text-styles-new.toml"));

            // When: Getting the styles through the default variant
            var styles = definition.Variants[0].TextStyles;

            // Then: The values are as expected
            Assert.That(styles[Models.TextSyle.Logo].FontName,Is.EqualTo("Logo Font"));
            Assert.That(styles[Models.TextSyle.Logo].FontColor,Is.EqualTo("111111"));
            Assert.That(styles[Models.TextSyle.BoxTitle].FontSize,Is.EqualTo(24));
            Assert.That(styles[Models.TextSyle.BoxTitle].FontName,Is.EqualTo("Segoe UI"));
        }

        [Test]
        public void RemapLocations()
        {
            // Given: Loading A definition with locations and boxes that reference them
            var definition = Loader.Load(GetStream("locations.toml"));

            // When: Getting primitives
            var primitives = definition.Variants[0].GeneratePrimitives(new TestImageSource()).ToList();

            // Then: Boxes have extents as expected
            Assert.That(primitives[1].Rectangle.Width,Is.EqualTo(400m).Within(0.01m));
            Assert.That(primitives[2].Rectangle.Height,Is.EqualTo(284m).Within(0.01m));
            Assert.That(primitives[3].Rectangle.Y,Is.EqualTo(476m).Within(0.01m));

            // TODO: Need to test numrows somehow
        }

        [Test]
        public void BoxTitleLanguage()
        {
            // Given: A definition with a localized variant
            var definition = Loader.Load(GetStream("lang.toml"));

            // When: Getting primitives for 2nd variant (which is localized)
            var primitives = definition.Variants[1].GeneratePrimitives(new TestImageSource()).ToList();

            Assert.Multiple(()=>
            {
                // Then: Box title is in language two
                Assert.That(
                    primitives
                    .Where(x=> x is TextPrimitive)
                    .Cast<TextPrimitive>()
                    .Where(x=>x.Style == Models.TextSyle.BoxTitle)
                    .Single().Text,
                    Is.EqualTo("title two")
                );

                // And: Logo title is in language two
                Assert.That(
                    primitives
                    .Where(x=> x is TextPrimitive)
                    .Cast<TextPrimitive>()
                    .Where(x=>x.Style == Models.TextSyle.Logo)
                    .Single().Text,
                    Is.EqualTo("logo two")
                );
            });

        }

        [Test]
        public void BoxTitleMainLanguage()
        {
            // Given: A definition with a localized variant
            var definition = Loader.Load(GetStream("lang.toml"));

            // When: Getting primitives for 1st variant (which is not localized)
            var primitives = definition.Variants[0].GeneratePrimitives(new TestImageSource()).ToList();

            Assert.Multiple(()=>
            {
                // Then: Box title is in language one
                Assert.That(
                    primitives
                    .Where(x=> x is TextPrimitive)
                    .Cast<TextPrimitive>()
                    .Where(x=>x.Style == Models.TextSyle.BoxTitle)
                    .Single().Text,
                    Is.EqualTo("title one")
                );

                // And: Logo title is in language two
                Assert.That(
                    primitives
                    .Where(x=> x is TextPrimitive)
                    .Cast<TextPrimitive>()
                    .Where(x=>x.Style == Models.TextSyle.Logo)
                    .Single().Text,
                    Is.EqualTo("logo one")
                );
            });
        }

        /// <summary>
        /// Scenario: Loader loads file with title
        /// </summary>
        [Test]
        public void DocumentTitleLanguage()
        {
            // When: Loading a definition with a localized title in a variant
            var definition = Loader.Load(GetStream("title.toml"));

            // Then: Title is localized when accessed through that variant
            Assert.That(definition.Variants[1].DocumentTitle, Is.EqualTo("Language Two"));
        }
    }
}
