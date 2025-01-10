using LogoSlideMaker.Configure;
using LogoSlideMaker.Export;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
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

public partial class MainViewModel(IGetImageAspectRatio bitmaps, ILogger<MainViewModel> logger): INotifyPropertyChanged
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
    /// View needs to give us a way to dispatch onto the UI Thread
    /// </summary>
    public Action<Action> UIAction { get; set; } = (x => x());

    /// <summary>
    /// Drawing primitives needed to render the current slide
    /// </summary>
    public IReadOnlyList<Primitive> Primitives => _primitives;

    /// <summary>
    /// Drawing primitives needed to render bounding boxes for the current slide
    /// </summary>
    public IReadOnlyList<Primitive> BoxPrimitives => _boxPrimitives;

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public IEnumerable<string> ImagePaths =>
        _definition?.Logos.Select(x => x.Value.Path).Concat(_definition.Files.Template.Bitmaps) ?? [];

    /// <summary>
    /// Current rendering configuration
    /// </summary>
    public RenderConfig? RenderConfig => _definition?.Render;

    /// <summary>
    /// Of the slides (variants) defined in the definition, which one are we showing
    /// First slide is zero (TODO: Should be '1')
    /// </summary>
    public int SlideNumber
    {
        get => _slideNumber;
        set
        {
            try
            {
                if (_definition is not null && value < _definition.Variants.Count && value != _slideNumber && value >= 0)
                {
                    _slideNumber = value;

                    PopulateLayout();
                    GeneratePrimitives();

                    OnPropertyChanged();
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
    private int _slideNumber = 0;

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
    public IEnumerable<string> SlideNames
    {
        get
        {
            if (_definition is null)
            {
                return ["N/A"];
            }
            if (_definition.Variants.Count == 0)
            {
                return ["Default"];
            }
            return _definition.Variants.Select((x,i) => $"{i}. {x.Name}");
        }
    }

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
            if (_definition is null)
            {
                return null;            
            }
            if (_definition.Files?.Output is not null)
            {
                // output path is relative to the current definition file
                var directory = Path.GetDirectoryName(LastOpenedFilePath) ?? "./";
                var result = Path.Combine(directory, _definition.Files.Output);
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
    public string DocumentTitle
    {
        get
        {
            if (_definition?.Layout.Title == null && LastOpenedFilePath == null)
            {
                return string.Empty;
            }
            if (_definition?.Layout.Title == null)
            {
                return Path.GetFileName(LastOpenedFilePath!);
            }
            return _definition.Layout.Title;
        }
    }

    /// <summary>
    /// Subtitle of the overall document (definition) being shown
    /// </summary>

    public string DocumentSubtitle
    {
        get
        {
            if (_definition is null)
            {
                return string.Empty;
            }
            if (_definition.Variants.Count == 0)
            {
                return "Only slide";
            }
            var variant = _definition.Variants[SlideNumber];

            if (SlideNumber < 0 || SlideNumber >= _definition.Variants.Count)
            {
                logFailSlideOutoFoRange(SlideNumber, _definition.Variants.Count);
                return "???";
            }

            var result = $"Slide {SlideNumber + 1} of {_definition.Variants.Count}";
            if (!string.IsNullOrWhiteSpace(variant.Name))
            {
                result += ": " + variant.Name;
            }
            if (variant.Description.Count > 0)
            {
                result += Environment.NewLine + string.Join(" / ", variant.Description);
            }
            return result;
        }
    }

    //    // [User Can] Turn visual display of bounding boxes on and off in preview
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
                    // TODO: This method is way too long

                    _primitives.Clear();
                    IsLoading = true;

                    var sr = new StreamReader(stream);
                    var toml = sr.ReadToEnd();

                    // This will throw if unable to parse
                    var loaded = Toml.ToModel<Definition>(toml);

                    // TODO: Need to consider the load the included logos here
                    // This points to a structural problem. At this point, we
                    // were given a stream, but we will need to load a FILE, and
                    // we need access to the original path, which will give us
                    // a relative directory starting point to find the include
                    // file.

                    // Parse includes
                    if (!string.IsNullOrWhiteSpace(loaded.Files.Include.Logos) && path is not null)
                    {
                        var dir = Path.GetDirectoryName(path);
                        var logopath = Path.Combine(dir!, loaded.Files.Include.Logos);

                        using var logostream = File.OpenRead(logopath);
                        using var logosr = new StreamReader(logostream);
                        var logotoml = logosr.ReadToEnd();
                        var logos = Toml.ToModel<Definition>(logotoml);

                        loaded.IncludeLogosFrom(logos);
                    }

                    loaded.ProcessAfterLoading();

                    _definition = loaded;
                    _gitVersion = null;
                    if (SlideNumber >= _definition.Variants.Count)
                    {
                        SlideNumber = 0;
                    }

                    PopulateLayout();

                    LastOpenedFilePath = path;
                    _gitVersion = path is not null ? Utilities.GitVersion.GetForDirectory(path) : null;

                    if (_definition?.Files.Output?.Contains("$Version") ?? false)
                    {
                        _definition.Files.Output = _definition.Files.Output.Replace("$Version", _gitVersion);
                    }

                    logOkDetails(DocumentTitle);

                    DefinitionLoaded?.Invoke(this, new EventArgs());
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
        if (_definition is null)
        {
            return;        
        }
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
        if (_definition is null)
        {
            return;
        }
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
    /// Generate and retain all primitives needed to display this slide
    /// </summary>
    /// <remarks>
    /// Note that we can't generate primitives until we've loaded and (more importantly)
    /// measured all the images
    /// </remarks>
    public void GeneratePrimitives()
    {
        _primitives.Clear();
        _boxPrimitives.Clear();

        if (_definition is null || _layout is null)
        {
            return;
        }

        var config = _definition.Render;

        // Add primitives for a background
        var bgRect = new Configure.Rectangle() { X = 0, Y = 0, Width = 1280, Height = 720 };

        // If there is a bitmap template, draw that
        var definedBitmaps = _definition.Files.Template.Bitmaps;
        var sourceBitmap = _layout.Variant.Source;
        if (definedBitmaps is not null && definedBitmaps.Count > sourceBitmap && bitmaps.Contains(definedBitmaps[sourceBitmap]))
        {
            _primitives.Add(new ImagePrimitive()
            {
                Rectangle = bgRect,
                Path = definedBitmaps[sourceBitmap]
            });
        }
        else
        {
            // Else Draw a white background
            _primitives.Add(new RectanglePrimitive()
            {
                Rectangle = bgRect,
                Fill = true
            });
        }

        // Add needed primitives for each logo
        var generator = new PrimitivesEngine(config, bitmaps);
        _primitives.AddRange(_layout.Logos.SelectMany(generator.ToPrimitives));

        // Add box title primitives
        _primitives.AddRange(_layout.Text.SelectMany(generator.ToPrimitives));

        // Add optional primitives to draw 
        _boxPrimitives.AddRange(
            _definition.Boxes
                .Where(x => x.Outer is not null)
                .Select(x => new RectanglePrimitive()
                {
                    Rectangle = x.Outer! with
                    {
                        X = x.Outer.X * 96m,
                        Y = x.Outer.Y * 96m,
                        Width = x.Outer.Width * 96m,
                        Height = x.Outer.Height * 96m
                    }
                }
                )
        );
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
            if (_definition is null)
            {
                return;
            }

            IsExporting = true;

            var exportPipeline = new ExportPipeline(_definition);

            var directory = LastOpenedFilePath is not null ? Path.GetDirectoryName(LastOpenedFilePath) : null;

            // TODO: Would be much better to do this when the definition is LOADED, because we'll
            // already be on a loading screen at that point!
            await exportPipeline.LoadAndMeasureAsync(directory);

            var templatePath = _definition.Files?.Template?.Slides;
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

            exportPipeline.Save(templateStream, outPath, _gitVersion);
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
    /// Create a slide layout for the current slide
    /// </summary>
    private void PopulateLayout()
    {
        if (_definition is null)
        {
            return;
        }

        var existingVariants = _definition.Variants;
        var slideNumber = _slideNumber;

        // In case of no variants, we'll use a blank
        if (existingVariants.Count == 0)
        {
            existingVariants = [new()];
        }

        if (slideNumber >= existingVariants.Count)
        {
            logWarningSlideOutoFoRange(slideNumber, existingVariants.Count);
            slideNumber = 0;
        }

        var variant = existingVariants[slideNumber];

        var engine = new LayoutEngine(_definition, variant);
        _layout = engine.CreateSlideLayout();
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
        UIAction(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }
    #endregion

    #region Fields

    private Definition? _definition;
    private SlideLayout? _layout;
    private string? _gitVersion;
    private readonly List<Primitive> _primitives = [];
    private readonly List<Primitive> _boxPrimitives = [];

    // Needed because .NET 8 can't get logger from default parameters. Will be fixed in .NET 9, so they say.
    private readonly ILogger _logger = logger;
    #endregion

    #region Logging

    // NOTE: These duplicate loggers in the main window. Should think about how to DRY this out!

    [LoggerMessage(Level = LogLevel.Information, EventId = 1000, Message = "{Location}: OK")]
    public partial void logOk([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1010, Message = "{Location}: {Moment} OK")]
    public partial void logOkMoment(string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1020, Message = "{Location}: OK {Details}")]
    public partial void logOkDetails(string details, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1030, Message = "{Location}: {Moment} OK {Path}")]
    public partial void logOkMomentPath(string moment, string path, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1040, Message = "{Location}: OK {Title} {Details}")]
    public partial void logOkTitleDetails(string title, string details, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1008, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1008, Message = "{Location}: Failed")]
    public partial void logFail([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1038, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(Exception ex, string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1038, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(string moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1058, Message = "{Location}: Failed to set value to {Value}")]
    public partial void logFailSetValue(Exception ex, object value, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Critical, EventId = 1009, Message = "{Location}: Critical failure")]
    public partial void logCritical(Exception ex, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 1031, Message = "{Location}: {Moment}")]
    public partial void logDebugMoment(string moment, [CallerMemberName] string? location = null);

    // Application-specific log messages follow

    [LoggerMessage(Level = LogLevel.Error, EventId = 1058, Message = "{Location}: Slide number {Number} out of range {Count}")]
    public partial void logFailSlideOutoFoRange(int number, int count, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 1078, Message = "{Location}: Slide number {Number} out of range {Count}")]
    public partial void logWarningSlideOutoFoRange(int number, int count, [CallerMemberName] string? location = null);

    #endregion
}
