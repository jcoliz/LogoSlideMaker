using LogoSlideMaker.Public;
using System.Reflection;

namespace LogoSlideMaker.Tests
{
    internal class PublicInterfaceTests
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

        private static Stream GetStream(string filename)
        {
            var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
            var resource = names.Where(x => x.Contains($".{filename}")).Single();
            var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

            return stream!;
        }
    }
}
