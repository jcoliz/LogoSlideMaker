using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System;
using System.Reflection;

public static class LoggerExtensions
{
    public static LoggerConfiguration WriteToLogAnalyticsIfConfigured(this LoggerConfiguration loggerConfiguration, out Guid sessionId)
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

        if (string.IsNullOrWhiteSpace(idOptions.AppSecret) || logsOptions.EndpointUri is null)
        {
            sessionId = Guid.Empty;
            return loggerConfiguration;
        }

        var credential = new ClientSecretCredential
        (
            tenantId: idOptions.TenantId.ToString(),
            clientId: idOptions.AppId.ToString(),
            clientSecret: idOptions.AppSecret
        );

        sessionId = Guid.NewGuid();
        return loggerConfiguration
            .Enrich.WithProperty("Session", sessionId)
            .WriteTo.AzureLogAnalytics
            (
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
    }
}