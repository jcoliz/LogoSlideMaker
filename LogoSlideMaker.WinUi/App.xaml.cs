using System;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
            .WriteTo.File(System.IO.Path.GetTempPath()+"/LogoSlideMaker/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("----------------------------------");

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

                    var bitmaps = new BitmapCache();
                    services.AddSingleton<IGetImageAspectRatio>(bitmaps);
                    services.AddSingleton(bitmaps);
                })

                .Build();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
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
