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
using Windows.Storage;

namespace LogoSlideMaker.WinUi.ViewModels;

public class MainViewModel(IGetImageAspectRatio bitmaps, ILogger<MainViewModel> logger): INotifyPropertyChanged
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
    public event EventHandler<ErrorEventArgs>? ErrorFound = delegate { };
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
                if (_definition is not null && value < _definition.Variants.Count && value != _slideNumber)
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
                logger.LogError(ex, "SlideNumber: Failed to advance to {value}", value);

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
                logger.LogError("DocumentSubtitle: Error slide number {Number} out of range {Count}", SlideNumber, _definition.Variants.Count);
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

    /// <summary>
    /// [User Can] Reload changes made in TOML file since last (re)load
    /// </summary>
    public ICommand Reload => _Reload ??= new RelayCommand(_ => ReloadDefinitionAsync().ContinueWith(_ => { }));
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
                    LoadDefinition(stream);
                    LastOpenedFilePath = path;
                }
                catch (TomlException ex)
                {
                    var args = new ErrorEventArgs() { Title = "TOML parsing failed", Details = ex.Message };
                    ErrorFound?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    // I would like to track what causes this, so as to perhaps create more detailed
                    // error-handling
                    logger.LogError(ex, "LoadDefinitionAsync: Stream load Failed");

                    var args = new ErrorEventArgs() { Title = "Opening file failed", Details = ex.Message };
                    ErrorFound?.Invoke(this, args);
                }
            });
            logger.LogDebug("LoadDefinitionAsync: Launched");
        }
        catch (Exception ex)
        {
            // TODO: Actually need to surface these to user. But for now, just dont crash

            logger.LogError(ex,"LoadDefinitionAsync: Failed");
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
        if (_definition is null || _layout is null)
        {
            return;
        }

        _primitives.Clear();
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

#if false
        // TODO: This will be user-configurable
        // Add bounding boxes for any boxes with explicit outer dimensions
        _primitives.AddRange(
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
#endif
    }

    /// <summary>
    /// Export current definition to presentation at <paramref name="outPath"/>
    /// </summary>
    /// <param name="outPath">Fully-qualified location of output presentation</param>
    public async Task ExportToAsync(string outPath)
    {
        if (_definition is null)
        {
            return;        
        }
        var exportPipeline = new ExportPipeline(_definition);

        var directory = Path.GetDirectoryName(LastOpenedFilePath)!;

        // TODO: Would be much better to do this when the definition is LOADED, because we'll
        // already be on a loading screen at that point!
        await exportPipeline.LoadAndMeasureAsync(directory);

        var templatePath = _definition.Files?.Template?.Slides;
        if (templatePath is not null) 
        { 
            templatePath = Path.Combine(directory, templatePath);
        }

        // TODO: Need a way to inject version (How are we even going to GET it??)
        exportPipeline.Save(templatePath, outPath, null);
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
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource)!;

        return stream;
    }

    /// <summary>
    /// Load a definition from the specified <paramref name="stream"/>
    /// </summary>
    /// <param name="stream">Stream of definition file</param>
    private void LoadDefinition(Stream stream)
    {
        try
        {
            _primitives.Clear();
            IsLoading = true;

            var sr = new StreamReader(stream);
            var toml = sr.ReadToEnd();
            
            // This will throw if unable to parse
            var loaded = Toml.ToModel<Definition>(toml);

            _definition = loaded;
            if (SlideNumber >= _definition.Variants.Count)
            {
                SlideNumber = 0;
            }

            PopulateLayout();

            logger.LogInformation("LoadDefinition: OK {title}", DocumentTitle);
        }
        finally
        {
            DefinitionLoaded?.Invoke(this, new EventArgs());
            OnPropertyChanged(nameof(DocumentTitle));
            OnPropertyChanged(nameof(DocumentSubtitle));
        }
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

        var variant = _definition.Variants.Count > 0 ? _definition.Variants[_slideNumber] : new Variant();

        var engine = new LayoutEngine(_definition, variant);
        _layout = engine.CreateSlideLayout();
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
    private readonly List<Primitive> _primitives = [];

    #endregion
}
