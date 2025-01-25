
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Templates;

try
{
    var configuration = new ConfigurationBuilder()
        .AddTomlFile
        (
            new EmbeddedFileProvider(Assembly.GetExecutingAssembly()), 
            "config.toml", 
            optional: false, 
            reloadOnChange: false
        )
        .Build();

    var logsOptions = new LogIngestionOptions();
    configuration.Bind(LogIngestionOptions.Section, logsOptions);

    var idOptions = new IdentityOptions();
    configuration.Bind(IdentityOptions.Section, idOptions);

    var endpoint = logsOptions.EndpointUri!.ToString();
    var appid = idOptions.AppId.ToString();

    Serilog.Debugging.SelfLog.Enable(Console.Error);

    var logConfig = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.WithProperty("Session", Guid.NewGuid())
        .WriteTo.Debug()
        .WriteTo.Console(
            new Serilog.Formatting.Json.JsonFormatter()
        )
        .WriteTo.AzureLogAnalytics(
//            new ExpressionTemplate(
//                "{ { SE: Session, SC: SourceContext, LO: Location, ID: EventId, LV: if @l = 'Information' then undefined() else @l, MT: @mt, EX: @x, PR: rest()} }\n"
//            ),
//            new Serilog.Formatting.Json.JsonFormatter(),
            new()
            {
                ClientId = appid,
                ClientSecret = idOptions.AppSecret,
                TenantId = idOptions.TenantId.ToString(),
                ImmutableId = logsOptions.DcrImmutableId,
                Endpoint = endpoint,
                StreamName = logsOptions.Stream
            },
            new()
            {
                BufferSize = 5000,
                BatchSize = 10,
//                MinLogLevel = Serilog.Events.LogEventLevel.Information,
            }
        );

    Log.Logger = logConfig.CreateLogger();

    var factory = new LoggerFactory().AddSerilog(Log.Logger);
    var _logger = factory.CreateLogger<Program>();

    _logger.LogInformation(1000,"Starting");
    _logger.LogDebug(1001,"Debug Info");
    _logger.LogInformation(1002,"Example Property {Property}","Hello");
    _logger.LogWarning(1003, new Exception( "Warning" ), "Sample Warning");
    _logger.LogError(1004, new Exception( "Error" ), "Sample Error");
    _logger.LogCritical(1005, new Exception( "Critical" ), "Sample Critical");
    _logger.LogInformation(1006,"Continue");
    _logger.LogInformation(1006,"Continue");
    _logger.LogInformation(1006,"Continue");

    Log.CloseAndFlush();
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERROR: {0}",ex.Message);
}

internal class LoadedConfig
{
    public IdentityOptions? LogAnalytics { get; set; }
}
