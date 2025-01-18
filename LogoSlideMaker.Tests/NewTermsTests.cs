using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using LogoSlideMaker.Tests.Helpers;
using System.Reflection;
using System.Xml.Serialization;

namespace LogoSlideMaker.Tests
{
    internal class NewTermsTests: TestsBase
    {
        /// <summary>
        /// Scenario: Loader loads file with variant
        /// </summary>
        [Test]
        public void LoadsOk()
        {
            // When: Loading a definition with one variant
            var definition = Loader.Load(GetStream("new-terms.toml"));

            // Then: Result has two variants
            Assert.That(definition.Variants, Has.Count.EqualTo(2));
        }
        /// <summary>
        /// Scenario: Remaps location using new terms
        /// </summary>
        [Test]
        public void RemapsLocations()
        {
            // When: Loading a definition with locations remapped using new system
            var definition = Loader.Load(GetStream("new-terms.toml")) as PublicDefinition;

            // Then: Boxes are located
            var boxes = definition!.Definition.Boxes.Select(x=>x.Outer!.X);
            Assert.That(boxes,Has.All.GreaterThan(0));
        }

        [Test]
        public void RendersBackgroundCorrectly()
        {
            // Given: Loaded a definition with a mapped background
            var definition = Loader.Load(GetStream("new-terms.toml"));

            // When: Generating primitives for the first slide
            var primitives = definition.Variants[0].GeneratePrimitives(new TestImageSource());

            // Then: Contains a correct background image
            Assert.That(primitives
                .Select(x=>x as ImagePrimitive)
                .Single(x=>x?.Purpose == PrimitivePurpose.Background)!
                .Path
                ,Is.EqualTo("1.png"));
        }
    }
}