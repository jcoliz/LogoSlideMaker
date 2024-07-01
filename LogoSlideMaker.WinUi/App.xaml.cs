using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

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
        this.InitializeComponent();

        //
        // Set up .NET generic host
        //
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host
        //
        _host = new HostBuilder()
            .ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                // Config files will be found in the content root path
                configurationBuilder.SetBasePath(context.HostingEnvironment.ContentRootPath);

                configurationBuilder.AddJsonFile("appsettings.json", optional:true);

                // Enable picking up configuration from the environment vars
                configurationBuilder.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Only really need ONE of these main window implementations.
                // Including both here so it's easy to switch between them
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
            })
            .ConfigureLogging((context, logging) =>
            {
                // Get log configuration out of `Logging` section in configuration
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                // Send logs to the console window
                // (Only useful if console window has been created)
                logging.AddConsole();

                // Send logs to debug console
                // (Only useful if running in Visual Studio)
                logging.AddDebug();
            })
            .Build();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;
    private readonly IHost _host;
}
