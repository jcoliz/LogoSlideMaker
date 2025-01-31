using LogoSlideMaker.Export;
using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using LogoSlideMaker.WinUi.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Tomlyn;
using Windows.ApplicationModel;
using Windows.Storage;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace LogoSlideMaker.WinUi.ViewModels;

public partial class MainViewModel(IGetImageAspectRatio bitmaps, IDispatcher dispatcher, ILogger<MainViewModel> logger) : INotifyPropertyChanged
{
    #region Events
    /// <summary>
    /// One of our properties has changed
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };

    /// <summary>
    /// A new definition has been loaded
    /// </summary>
    public event EventHandler<EventArgs>? DefinitionLoaded = delegate { };

    /// <summary>
    /// User action has resulted in an error
    /// </summary>
    public event EventHandler<UserErrorEventArgs>? ErrorFound = delegate { };
    #endregion

    #region Properties

    /// <summary>
    /// The definition we are currently showing
    /// </summary>
    public IDefinition Definition => _definition;

    /// <summary>
    /// Size of the drawing area
    /// </summary>
    public System.Drawing.Size PlatenSize { get; } = new(1280, 720);

    /// <summary>
    /// Drawing primitives needed to render the current slide
    /// </summary>
    public IReadOnlyList<Primitive> Primitives => _primitives;

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public IEnumerable<string> ImagePaths => _definition.ImagePaths;

    /// <summary>
    /// The styles which should be used to render text on this slide
    /// </summary>
    public IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles => _currentVariant is not null ? _currentVariant.TextStyles : new Dictionary<TextSyle, ITextStyle>();

    /// <summary>
    /// Of the slides (variants) defined in the definition, which one are we showing
    /// First slide is zero (TODO: Should be '1')
    /// </summary>
    public int SlideNumber
    {
        get => _currentVariant.Index;
        set
        {
            try
            {
                if (value < _definition.Variants.Count && value != _currentVariant.Index && value >= 0)
                {
                    _currentVariant = _definition.Variants[value];

                    // TODO: How do we know that the images have been loaded??
                    GeneratePrimitives();

                    OnPropertyChanged();                    
                    OnPropertyChanged(nameof(DocumentTitle));
                    OnPropertyChanged(nameof(DocumentSubtitle));
                }
            }
            catch (Exception ex)
            {
                // Note that we now have a consistency problem. We have tried to update the slide, so
                // we have a new number, BUT it failed to take! So we are in an inconsistent state.
                logFailSetValue(ex, value);
            }
        }
    }

    /// <summary>
    /// Whether we are undergoing a loading operation
    /// </summary>
    public bool IsLoading
    {
        get => _IsLoading;
        set
        {
            if (value != _IsLoading)
            {
                _IsLoading = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _IsLoading = true;


    /// <summary>
    /// Whether we are currently exporting a presentation
    /// </summary>
    public bool IsExporting
    {
        get => _IsExporting;
        set
        {
            if (value != _IsExporting)
            {
                _IsExporting = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _IsExporting = false;

    /// <summary>
    /// Display names of the slides
    /// </summary>
    /// <remarks>
    /// This is for a future slide picker
    /// </remarks>
    public IEnumerable<string> SlideNames =>
        _definition.Variants.Select((x, i) => $"{i}. {x.Name}");

    /// <summary>
    /// Path to input file, last time we opened a file from file system
    /// </summary>
    public string? LastOpenedFilePath
    {
        get => (string?)ApplicationData.Current.LocalSettings.Values[nameof(LastOpenedFilePath)];
        set
        {
            if (value != (string?)ApplicationData.Current.LocalSettings.Values[nameof(LastOpenedFilePath)])
            {
                ApplicationData.Current.LocalSettings.Values[nameof(LastOpenedFilePath)] = value;
                OnPropertyChanged(nameof(DocumentTitle));
            }
        }
    }

    /// <summary>
    /// Default file to which exported slides should be output
    /// </summary>
    public string? OutputPath
    {
        get
        {
            if (_definition.OutputFileName is not null)
            {
                // output path is relative to the current definition file
                var directory = Path.GetDirectoryName(LastOpenedFilePath) ?? "./";
                var result = Path.Combine(directory, _definition.OutputFileName);

                if (result?.Contains("$Version") ?? false)
                {
                    result = result.Replace("$Version", _gitVersion);
                }

                return result;
            }
            else
            {
                // output path is exactly the input path, with the extension replaced to pptx
                var directory = Path.GetDirectoryName(LastOpenedFilePath) ?? "./";
                var file = Path.GetFileNameWithoutExtension(LastOpenedFilePath) ?? Path.GetRandomFileName();
                var result = Path.Combine(directory, file + ".pptx");
                return result;
            }
        }
    }

    /// <summary>
    /// Name of the overall document (definition) being shown
    /// </summary>
    public string DocumentTitle => (_currentVariant.DocumentTitle, LastOpenedFilePath) switch
    {
        (null, null) => string.Empty,
        (null, _) => Path.GetFileName(LastOpenedFilePath),
        _ => _currentVariant.DocumentTitle
    };

    /// <summary>
    /// Subtitle of the overall document (definition) being shown
    /// </summary>

    public string DocumentSubtitle
    {
        get
        {
            var result = $"Slide {_currentVariant.Index + 1} of {_definition.Variants.Count}";
            if (!string.IsNullOrWhiteSpace(_currentVariant.Name))
            {
                result += ": " + _currentVariant.Name;
            }
            if (_currentVariant.Description.Count > 0)
            {
                result += Environment.NewLine + string.Join(" / ", _currentVariant.Description);
            }
            return result;
        }
    }
    /// <summary>
    /// Turn visual display of bounding boxes on and off 
    /// </summary>
    public bool ShowBoundingBoxes
    {
        get => _ShowBoundingBoxes;
        set
        {
            if (value != _ShowBoundingBoxes)
            {
                _ShowBoundingBoxes = value;
                OnPropertyChanged();
            }
        }    
    }
    private bool _ShowBoundingBoxes = false;

    public static string AppVersion
    {
        get
        {
            var version = Package.Current?.Id.Version;
            if (version is null)
            {
                return "N/A";
            }
            return $"{version.Value.Major}.{version.Value.Minor}.{version.Value.Build}.{version.Value.Revision}";
        }
    }
    public string BuildVersion
    {
        get
        {
            _BuildVersion ??= LoadBuildVersion();
            return _BuildVersion ?? string.Empty;
        }
    }
    private string? _BuildVersion;

    public static string AppDisplayName => Package.Current?.DisplayName ?? "N/A";

    public static string LogsFolder
    {
        get
        {
            var tempfolder = ApplicationData.Current.TemporaryFolder.Path;
            var result = Path.Combine(tempfolder, "Logs");
            return result;
        }
    }

    /// <summary>
    /// [User Can] Reload changes made in TOML file since last (re)load
    /// </summary>
    public ICommand Reload => _Reload ??= new RelayCommand(_ => ReloadDefinitionAsync().ContinueWith(t =>
    {
        if (t.Exception is not null)
        {
            logFail(t.Exception);
        }
    }));
    private ICommand? _Reload = null;

    /// <summary>
    /// [User Can] Advance the preview to the next available slide
    /// </summary>
    public ICommand NextSlide => _NextSlide ??= new RelayCommand(_ => AdvanceToNextSlide());
    private ICommand? _NextSlide = null;

    /// <summary>
    /// [User Can] Rewind the preview to the previous slide
    /// </summary>
    public ICommand PreviousSlide => _PreviousSlide ??= new RelayCommand(_ => BackToPreviousSlide());
    private ICommand? _PreviousSlide = null;
#endregion

    #region Methods

    /// <summary>
    /// Load a definition from the specified <paramref name="path"/>
    /// </summary>
    /// <param name="path">Where to look for a definition file</param>
    public async Task LoadDefinitionAsync(string? path)
    {
        try
        {
            using var stream = path is null ? OpenEmbeddedDefinition() : File.OpenRead(path);
            await Task.Run(() => 
            {
                try
                {
                    _primitives.Clear();
                    IsLoading = true;

                    var currentSlideIndex = _currentVariant?.Index ?? 0;

                    _definition = Loader.Load(stream, path is not null ? Path.GetDirectoryName(path) : null);
                    _gitVersion = null;
                    _currentVariant = _definition.Variants[currentSlideIndex < _definition.Variants.Count ? currentSlideIndex : 0];

                    LastOpenedFilePath = path;
                    _gitVersion = path is not null ? Utilities.GitVersion.GetForDirectory(path) : null;

                    logOkDetails(DocumentTitle);

                    DefinitionLoaded?.Invoke(this, new EventArgs());
                    OnPropertyChanged(nameof(Definition));

                    // TODO: Could I change to Defintion.Title, to just have one property to manage here?
                    OnPropertyChanged(nameof(DocumentTitle));
                    OnPropertyChanged(nameof(DocumentSubtitle));
                }
                catch (TomlException ex)
                {
                    var args = new UserErrorEventArgs() { Title = "TOML parsing failed", Details = ex.Message };
                    ErrorFound?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    // I would like to track what causes this, so as to perhaps create more detailed
                    // error-handling
                    logFailMoment(ex, "Stream load");

                    var args = new UserErrorEventArgs() { Title = "Opening file failed", Details = ex.Message };
                    ErrorFound?.Invoke(this, args);
                }
            });

            logDebugMoment("Launched");
        }
        catch (FileNotFoundException ex)
        {
            LastOpenedFilePath = null;

            var args = new UserErrorEventArgs() { Title = "File not found", Details = ex.Message };
            ErrorFound?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            // TODO: Actually need to surface these to user. But for now, just dont crash

            logFail(ex);
        }
    }

    /// <summary>
    /// Reload the last opened definition
    /// </summary>
    /// <returns></returns>
    public async Task ReloadDefinitionAsync()
    {
        await LoadDefinitionAsync(LastOpenedFilePath);
    }

    /// <summary>
    /// Show the next slide in sequence
    /// </summary>
    public void AdvanceToNextSlide()
    {
        if (SlideNumber >= _definition.Variants.Count - 1)
        {
            SlideNumber = 0;
        }
        else
        {
            ++SlideNumber;
        }
    }

    /// <summary>
    /// Show the previous slide in sequence
    /// </summary>
    public void BackToPreviousSlide()
    {
        if (SlideNumber <= 0)
        {
            SlideNumber = _definition.Variants.Count - 1;
        }
        else
        {
            --SlideNumber;
        }
    }

    /// <summary>
    /// Export current definition to presentation at <paramref name="outPath"/>
    /// </summary>
    /// <param name="outPath">Fully-qualified location of output presentation</param>
    public async Task ExportToAsync(string outPath)
    {
        var templateStream = default(Stream);
        try
        {
            IsExporting = true;

            var directory = LastOpenedFilePath is not null ? Path.GetDirectoryName(LastOpenedFilePath) : null;

            var templatePath = _definition.TemplateSlidesFileName;
            if (templatePath is not null)
            {
                if (directory is not null)
                {
                    templatePath = Path.Combine(directory, templatePath);
                    if (!File.Exists(templatePath))
                    {
                        throw new UserErrorException("Export failed", $"Template not found at {templatePath}");
                    }
                    templateStream = File.OpenRead(templatePath);
                }
                else
                {
                    templateStream = OpenEmbeddedFile(templatePath);
                }
            }

            //
            // LOAD IMAGES
            //

            var imageCache = new ImageCache() { BaseDirectory = directory };
            await imageCache.LoadAsync(_definition.ImagePaths);

            //
            // EXPORT
            //

            ExportPipelineEx.Export(_definition, imageCache, templateStream, outPath, _gitVersion);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new UserErrorException("Export failed", ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            throw new UserErrorException("Export failed", ex.Message);
        }
        catch
        {
            // Code errors can get logged by the parent
            throw;
        }
        finally
        {
            templateStream?.Dispose();

            IsExporting = false;
        }
    }

    public void GeneratePrimitives()
    {
        var latest = _currentVariant.GeneratePrimitives(bitmaps);
        _primitives.Clear();
        _primitives.AddRange(latest);
    }

    #endregion

    #region Internals

    /// <summary>
    /// Open the default definition embedded into the app
    /// </summary>
    /// <returns>Stream to emdedded definition</returns>
    private static Stream OpenEmbeddedDefinition()
    {
        var filename = "sample.toml";

        return OpenEmbeddedFile(filename) ?? throw new KeyNotFoundException($"Cannot find {filename}");
    }

    private static Stream? OpenEmbeddedFile(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).SingleOrDefault();
        if (resource is null)
        {
            return null;
        }
        return Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource)!;
    }

    /// <summary>
    /// Load the version of the app as known to the build system
    /// </summary>
    /// <remarks>
    /// Build system generates version.txt which has an app version in that.
    /// </remarks>
    private static string? LoadBuildVersion()
    {
        var stream = OpenEmbeddedFile("version.txt");
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }

    /// <summary>
    /// Raise the property changed event for <paramref name="propertyName"/>
    /// </summary>
    /// <param name="propertyName">Name of property that changed</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Raise the PropertyChanged event, passing the name of the property whose value has changed.
        // And be sure to do it on UI thread, because we may be running on a BG thread, and the
        // handler is often the framework, which runs on UI thread
        dispatcher.Dispatch(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }
    #endregion

    #region Fields
    private IDefinition _definition = Loader.Empty();
    private IVariant _currentVariant = Loader.Empty().Variants[0];
    private string? _gitVersion;
    private readonly List<Primitive> _primitives = [];
    #endregion

    #region Logging

    // NOTE: These duplicate loggers in the main window. Should think about how to DRY this out!

    [LoggerMessage(Level = LogLevel.Information, EventId = 1000, Message = "{Location}: OK")]
    public partial void logOk([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 1010, Message = "{Location}: {Moment} OK")]
    public partial void logOkMoment(string moment, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 1020, Message = "{Location}: OK {Details}")]
    public partial void logOkDetails(string details, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 1030, Message = "{Location}: {Moment} OK {Path}")]
    public partial void logOkMomentPath(string moment, string path, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 1040, Message = "{Location}: OK {Title} {Details}")]
    public partial void logOkTitleDetails(string title, string details, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1008, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1018, Message = "{Location}: Failed")]
    public partial void logFail([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1038, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(Exception ex, string moment, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1048, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(string moment, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1058, Message = "{Location}: Failed to set value to {Value}")]
    public partial void logFailSetValue(Exception ex, object value, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Critical, EventId = 1009, Message = "{Location}: Critical failure")]
    public partial void logCritical(Exception ex, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 1031, Message = "{Location}: {Moment}")]
    public partial void logDebugMoment(string moment, [CallerMemberName] string? location = "");

    // Application-specific log messages follow

    [LoggerMessage(Level = LogLevel.Error, EventId = 1059, Message = "{Location}: Slide number {Number} out of range {Count}")]
    public partial void logFailSlideOutoFoRange(int number, int count, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Warning, EventId = 1078, Message = "{Location}: Slide number {Number} out of range {Count}")]
    public partial void logWarningSlideOutoFoRange(int number, int count, [CallerMemberName] string? location = "");

    #endregion
}
