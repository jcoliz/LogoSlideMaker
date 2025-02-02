using LogoSlideMaker.Public;
using System.ComponentModel;

namespace LogoSlideMaker.WinUi.ViewModels;

/// <summary>
/// The minimal of viewmodel information we need in order to render a slide
/// </summary>
public interface IRenderViewModel: INotifyPropertyChanged
{
    IDefinition Definition { get; }

    IVariant Variant { get; }

    bool ShowBoundingBoxes { get; }

    // Needed because the renderer will be loading images from this path
    public string? LastOpenedFilePath { get; }

    public bool IsLoading { get; set; }
}
