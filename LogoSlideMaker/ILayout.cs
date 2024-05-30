public interface ILayout: IEnumerable<BoxLayout>
{
    string Name { get; }
    IEnumerable<string> Description { get; }
}