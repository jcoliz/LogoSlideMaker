using LogoSlideMaker.Configure;
using LogoSlideMaker.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogoSlideMaker.Public;

internal class PublicVariant(Variant variant) : IVariant
{
    public string Name => variant.Name;

    public ICollection<string> Description => variant.Description;

    public ICollection<Primitive> GeneratePrimitives()
    {
        throw new NotImplementedException();
    }
}
