namespace LogoSlideMaker.Public;

public interface IDefinition
{
    string? Title { get; }
    ICollection<IVariant> Variants { get; }
}
