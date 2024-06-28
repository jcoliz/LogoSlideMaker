using Mono.Options;

public class AppOptions: OptionSet
{

    public bool Help { get; private set; }
    public string? Input { get; private set; }
    public string? Output { get; private set; }
    public string? Template { get; private set; }
    public string? Version { get; private set; }
    public bool Exit { get; private set; }
    public bool Listing { get; private set; }

    public AppOptions()
    {
        Add("h|help", "show this message and exit", v => Help = v != null);
        Add("i|input=", "the {toml} containing slide definitions", v => Input = v);
        Add("o|output=", "the {pptx} where the slides will be written", v => Output = v);
        Add("t|template=", "the {template} to base the news slides on (optional)", v => Template = v);
        Add("l|list", "also print a listing of logos", v => Listing = v != null);
        Add("v|version=", "add the specified {version} identifier to slide notes (optional)", v => Version = v);
    }

    public new List<string> Parse(IEnumerable<string> args)
    {
        var result = base.Parse(args);

        String? error = null;

        if (string.IsNullOrWhiteSpace(Input))
        {
            error = "Must specify input file";
        }

        if (Help || error is not null)
        {
            Console.WriteLine($"{AppName} {AppVersion}");
            WriteOptionDescriptions(Console.Out);

            if (error is not null)
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {error}");
            }

            Exit = true;
        }

        return result;
    }

    static public string AppName => typeof(AppOptions).Assembly.GetName().Name ?? string.Empty;
    static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version?.ToString() ?? string.Empty;
}
