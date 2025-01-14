namespace LogoSlideMaker.Public;

public interface IDefinition
{
    string? Title { get; }
    ICollection<IVariant> Variants { get; }

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public ICollection<string> ImagePaths { get; }
}
