﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Runtime.CompilerServices;
using LogoSlideMaker.WinUi.Pickers;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Sinks.AzureLogAnalytics;
using Serilog.Templates;
using Windows.Storage;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace LogoSlideMaker.WinUi;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(MainViewModel.LogsFolder+"/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteToLogAnalyticsIfConfigured(out var sessionId)
#if DEBUG
            .WriteTo.File(
                new ExpressionTemplate(
                    "{ { TM: UtcDateTime(@t), SE: Session, SC: SourceContext, LO: Location, ID: EventId, LV: if @l = 'Information' then undefined() else @l, MT: @mt, EX: @x, PR: rest()} }\n"
                ),
                MainViewModel.LogsFolder+"/log-.json",
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                rollingInterval: RollingInterval.Day
            )
#endif
            .CreateLogger();

        try
        {
            var factory = new LoggerFactory().AddSerilog(Log.Logger);
            _logger = factory.CreateLogger<App>();

            logHello(sessionId);

            logOsVersion(Environment.OSVersion.Version.ToString());

            try
            {
                var mo = new ManagementObject("Win32_OperatingSystem=@");
                if (mo["SerialNumber"] is string serial)
                {
                    logMachineSerial(serial);
                }

                // https://github.com/microsoft/WindowsAppSDK/issues/4840
                //var token = HardwareIdentification.GetPackageSpecificToken(null);
                //var base64 = CryptographicBuffer.EncodeToBase64String(token.Id);
                //logMachineSerial(base64);
            }
            catch(Exception ex)
            {
                logFailMoment(ex, "Machine Serial");
            }

            Application.Current.UnhandledException += Application_UnhandledException;

            InitializeComponent();

            //
            // Set up .NET generic host
            //
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host
            //
            _host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSerilog();

                    services.AddSingleton<BitmapCache>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<IRenderViewModel>(sp => sp.GetRequiredService<MainViewModel>());
                    services.AddSingleton<DisplayRenderer>();
                    services.AddSingleton<MainWindow>();

                    services.AddSingleton(x => new Lazy<Window>(() => x.GetRequiredService<MainWindow>()));
                    services.AddSingleton<IDispatcher, Dispatcher>();

                    services.AddTransient<PickerFactory>();

                    logOkMoment("ConfigureServices");
                })

                .Build();

            logOk();
        }
        catch (Exception ex)
        {
            logCritical(ex);
        }
    }

    public IServiceProvider Services => _host!.Services;

    private void Application_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is Microsoft.UI.Xaml.Markup.XamlParseException pex)
        {
            logXamlParseException(pex, pex.StackTrace, pex.Source);
            if (pex.InnerException is not null)
            {
                logCritical(pex.InnerException);
            }
        }
        else
        {
            logCritical(e.Exception);
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            logStarting();

            if (_host is null)
            {
                throw new Exception("Host not found");            
            }

            logLocalFolder(ApplicationData.Current.LocalFolder.Path);

            await _host.StartAsync();

            m_window = _host.Services.GetService<MainWindow>();
            m_window!.Activate();

        }
        catch (Exception ex)
        {
            logCritical(ex);
            await Log.CloseAndFlushAsync();
        }
    }

    private Window? m_window;
    private readonly IHost? _host;
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "This is to quiet logger message warnings")]
    private ILogger _logger = NullLogger.Instance;

    [LoggerMessage(Level = LogLevel.Information, EventId = 100, Message = "---------------------------------- {Location}: Starting session {SessionId}")]
    public partial void logHello(Guid sessionId,[CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 110, Message = "{Location}: OK")]
    public partial void logOk([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 130, Message = "{Location}: {Moment} OK")]
    public partial void logOkMoment(string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 101, Message = "{Location}: OS Version {OsVersion}")]
    public partial void logOsVersion(string osversion, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 102, Message = "{Location}: Machine Serial # {SerialNum}")]
    public partial void logMachineSerial(string serialnum, [CallerMemberName] string? location = null);
    
    [LoggerMessage(Level = LogLevel.Debug, EventId = 120, Message = "{Location}: Starting")]
    public partial void logStarting([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 121, Message = "{Location}: Local Folder: {Path}")]
    public partial void logLocalFolder(string path, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 108, Message = "{Location}: Failed")]
    public partial void logFail([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 118, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(Exception ex, string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 128, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Critical, EventId = 109, Message = "{Location}: Critical failure")]
    public partial void logCritical(Exception ex, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Critical, EventId = 199, Message = "{Location}: Unhandled XamlParseException Stack: {Stack} Source: {Source}")]
    public partial void logXamlParseException(Exception ex, string? stack, string? source, [CallerMemberName] string? location = null);
}

internal class LoadedConfig
{
    public AzureLogAnalyticsOptions? LogAnalytics { get; set; }
}

internal class AzureLogAnalyticsOptions
{
    public LoggerCredential? Credentials { get; set; }
    public ConfigurationSettings? ConfigSettings { get; set; }
}