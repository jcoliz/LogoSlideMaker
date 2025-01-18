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
    }
}