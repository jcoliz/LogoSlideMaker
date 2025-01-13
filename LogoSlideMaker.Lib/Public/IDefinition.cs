namespace LogoSlideMaker.Public;

public interface IDefinition
{
    ICollection<IVariant> Variants { get; }
}
