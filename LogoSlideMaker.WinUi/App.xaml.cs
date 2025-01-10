using System;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Formatting.Compact;
using Windows.Storage;

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
            .Enrich.WithProperty("Session", Guid.NewGuid())
            .WriteTo.Debug()
            .WriteTo.File(MainViewModel.LogsFolder+"/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.File(new CompactJsonFormatter(),MainViewModel.LogsFolder+"/log-.json", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("----------------------------------");

            Application.Current.UnhandledException += Application_UnhandledException;

            this.InitializeComponent();

            //
            // Set up .NET generic host
            //
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host
            //
            _host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSerilog();

                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainViewModel>();

                    services.AddSingleton<BitmapCache>();
                    services.AddSingleton<IGetImageAspectRatio>(x => x.GetRequiredService<BitmapCache>());

                    Log.Debug("Startup: ConfigureServices OK");
                })

                .Build();

            Log.Debug("Startup: Build OK");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
        }
    }

    private void Application_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is Microsoft.UI.Xaml.Markup.XamlParseException pex)
        {
            Log.Fatal(e.Exception, "Unhandled XamlParseException Stack: {Stack} Source: {Source}", pex.StackTrace ?? "null", pex.Source ?? "null");
            if (pex.InnerException is not null)
            {
                Log.Fatal(pex.InnerException, "Inner exception");
            }
        }
        else
        {
            Log.Fatal(e.Exception, "Unhandled exception");
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
            Log.Information("Starting");

            if (_host is null)
            {
                throw new Exception("Host not found");            
            }

            var folder = ApplicationData.Current.LocalFolder;
            Log.Debug("Local Folder: {Path}", folder.Path);

            await _host.StartAsync();

            m_window = _host.Services.GetService<MainWindow>();
            m_window!.Activate();

        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            Log.CloseAndFlush();
        }
    }

    private Window? m_window;
    private readonly IHost? _host;
}
