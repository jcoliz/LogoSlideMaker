using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using LogoSlideMaker.Tests.Helpers;
using System.Reflection;
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
            Assert.That(definition.Title, Is.Null);
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
            Assert.That(definition.Title, Is.EqualTo("Document Title"));
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

            // Then: Has 2 Image Primitives
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
    }
}
