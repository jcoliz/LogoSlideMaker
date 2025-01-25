
using System.Reflection;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Serilog;

try
{
    var configuration = new ConfigurationBuilder()
        .AddTomlFile
        (
            new EmbeddedFileProvider(Assembly.GetExecutingAssembly()), 
            "config.toml", 
            optional: true,
            reloadOnChange: false
        )
        .Build();

    var logsOptions = new LogIngestionOptions();
    configuration.Bind(LogIngestionOptions.Section, logsOptions);

    var idOptions = new IdentityOptions();
    configuration.Bind(IdentityOptions.Section, idOptions);

    var credential = new ClientSecretCredential
    (
        tenantId: idOptions.TenantId.ToString(),
        clientId: idOptions.AppId.ToString(),
        clientSecret: idOptions.AppSecret
    );

    Serilog.Debugging.SelfLog.Enable(Console.Error);

    if (logsOptions.EndpointUri is null)
    {
        throw new Exception("Must specify logs endpoint URI");
    }

    var logConfig = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.WithProperty("Session", Guid.NewGuid())
        .WriteTo.Debug()
        .WriteTo.Console(
            new Serilog.Formatting.Json.JsonFormatter()
        )
        .WriteTo.AzureLogAnalytics(
            new()
            {
                ImmutableId = logsOptions.DcrImmutableId,
                Endpoint = logsOptions.EndpointUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped),
                StreamName = logsOptions.Stream,
                TokenCredential = credential
            },
            new()
            {
                BufferSize = 5000,
                BatchSize = 10,
                MinLogLevel = Serilog.Events.LogEventLevel.Information,
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

    await Log.CloseAndFlushAsync();

    // Needs time to settle before process is terminated
    await Task.Delay(TimeSpan.FromSeconds(2));
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERROR: {0}",ex.Message);
}
