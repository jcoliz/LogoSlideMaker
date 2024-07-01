using LogoSlideMaker.Configure;
using LogoSlideMaker.Export;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
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

internal class MainViewModel(IGetImageAspectRatio bitmaps): INotifyPropertyChanged
{
    #region Events
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    #endregion

    #region Properties
    /// <summary>
    /// View needs to give us a way to dispatch onto the UI Thread
    /// </summary>
    public Action<Action> UIAction { get; set; } = (x => x());

    public IReadOnlyList<Primitive> Primitives => _primitives;

    public IEnumerable<string> ImagePaths =>
        _definition?.Logos.Select(x => x.Value.Path).Concat(_definition.Files.Template.Bitmaps) ?? [];

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
            if (_definition is not null && value < _definition.Variants.Count && value != _slideNumber)
            {
                _slideNumber = value;

                PopulateLayout();
                GeneratePrimitives();
                OnPropertyChanged();
            }
        }
    }
    private int _slideNumber = 0;

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
                var directory = Path.GetDirectoryName(lastOpenedFilePath) ?? "./";
                var result = Path.Combine(directory, _definition.Files.Output);
                return result;
            }
            else
            {
                // output path is exactly the input path, with the extension replaced to pptx
                var directory = Path.GetDirectoryName(lastOpenedFilePath) ?? "./";
                var file = Path.GetFileNameWithoutExtension(lastOpenedFilePath) ?? Path.GetRandomFileName();
                var result = Path.Combine(directory, file + ".pptx");
                return result;
            }
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

    public async Task LoadDefinitionAsync(string? path)
    {
        lastOpenedFilePath = path;
        using var stream = path is null ? OpenEmbeddedDefinition() : File.OpenRead(path);
        await Task.Run(() => { LoadDefinition(stream); });
    }

    public void LoadDefinition(Stream stream)
    {
        _primitives.Clear();
        IsLoading = true;

        var sr = new StreamReader(stream);
        var toml = sr.ReadToEnd();
        _definition = Toml.ToModel<Definition>(toml);

        PopulateLayout();

        // Note that the view listens for this, and does its
        // part after we're done loading, including a call back to
        // GeneratePrimitives()
        IsLoading = false;
    }

    public async Task ReloadDefinitionAsync()
    {
        await LoadDefinitionAsync(lastOpenedFilePath);
    }

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

        var directory = Path.GetDirectoryName(lastOpenedFilePath)!;

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

    private Stream OpenEmbeddedDefinition()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource)!;

        return stream;
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Raise the PropertyChanged event, passing the name of the property whose value has changed.
        // And be sure to do it on UI thread, because we may be running on a BG thread
        UIAction(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }
    #endregion

    #region Internal Properties

    public string? lastOpenedFilePath
    {
        get => (string?)ApplicationData.Current.LocalSettings.Values[nameof(lastOpenedFilePath)];
        set
        {
            if (value != (string?)ApplicationData.Current.LocalSettings.Values[nameof(lastOpenedFilePath)])
            {
                ApplicationData.Current.LocalSettings.Values[nameof(lastOpenedFilePath)] = value;
            }
        }
    }
    #endregion

    #region Fields

    private Definition? _definition;
    private SlideLayout? _layout;
    private readonly List<Primitive> _primitives = [];

    #endregion
}
