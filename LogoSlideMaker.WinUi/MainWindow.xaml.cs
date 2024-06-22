using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using LogoSlideMaker.Configure;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Tomlyn;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LogoSlideMaker.WinUi;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Definition? _definition;
    private Layout.Layout? _layout;

    public MainWindow()
    {
        this.InitializeComponent();
        this.LoadResources();
    }

    private void LoadResources()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        _definition = Toml.ToModel<Definition>(toml);

        _layout = new Layout.Layout(_definition, new Variant());
        _layout.Populate();
    }

    void CanvasControl_Draw(
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender,
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        // Draw a white background
        args.DrawingSession.FillRectangle(new Rect() { X = 96, Y = 96, Width = 1280, Height = 720 }, Microsoft.UI.Colors.White);

        // Render each logo in the layout

        if (_definition is null)
        {
            return;        
        }

        var config = _definition.Render;
        foreach (var boxlayout in _layout)
        {
            foreach (var logolayout in boxlayout.Logos)
            {
                var logo = logolayout.Logo;

                if (logo is null)
                {
                    continue;
                }

                {
                    //using var stream = new FileStream(logo.Path, FileMode.Open);
                    //var doc = await CanvasSvgDocument.LoadAsync(sender, (Windows.Storage.Streams.IRandomAccessStream)stream);

                    //doc.
                    //args.DrawingSession.DrawSvg( (doc);
                }

                //var pic = target.OfType<IPicture>().Last();

                // Adjust size of icon depending on size of source image. The idea is all
                // icons occupy the same number of pixel area

                var aspect = 1.0m; // pic.Width / pic.Height;
                var width_factor = (decimal)Math.Sqrt((double)aspect);
                var height_factor = 1.0m / width_factor;
                var icon_width = config.IconSize * width_factor * logo.Scale;
                var icon_height = config.IconSize * height_factor * logo.Scale;

                var X = (logolayout.X - icon_width / 2.0m) * config.Dpi;
                var Y = (logolayout.Y - icon_height / 2.0m) * config.Dpi;
                var Width = icon_width * config.Dpi;
                var Height = icon_height * config.Dpi;

                // Draw a placeholder logo
                args.DrawingSession.DrawRectangle(new Rect() { X = 96.0f + (float)X, Y = 96.0f + (float)Y, Width = (float)Width, Height = (float)Height }, Microsoft.UI.Colors.Red, 1);

                var text_width_inches = logo.TextWidth ?? config.TextWidth;

                var text_x = (logolayout.X - text_width_inches / 2.0m) * config.Dpi;
                var text_y = (logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi;
                var text_width = text_width_inches * config.Dpi;
                var text_height = config.TextHeight * config.Dpi;

                //target.AddRectangle(100, 100, 100, 100);
                //var shape = target.Last();

                var shape = new Rect();
                shape.X = 96.0f + (float)text_x;
                shape.Y = 96.0f + (float)text_y;
                shape.Width = (float)text_width;
                shape.Height = (float)text_height;

                // Draw a placeholder textbox
                args.DrawingSession.DrawRectangle(shape, Microsoft.UI.Colors.Blue, 1);

                var tf = new CanvasTextFormat() { FontSize = config.FontSize * 96.0f / 72.0f, FontFamily = config.FontName, VerticalAlignment = CanvasVerticalAlignment.Center, HorizontalAlignment = CanvasHorizontalAlignment.Center };
                args.DrawingSession.DrawText(logo.Title, shape, new CanvasSolidColorBrush(sender,Microsoft.UI.Colors.Black), tf);

#if false
                var tf = shape.TextFrame;
                tf.Text = logo.Title;
                tf.LeftMargin = 0;
                tf.RightMargin = 0;
                var font = tf.Paragraphs.First().Portions.First().Font;

                font.Size = config.FontSize;
                font.LatinName = config.FontName;
                font.Color.Update(config.FontColor);
                shape.Fill.SetNoFill();
                shape.Outline.SetNoOutline();
#endif
            }
        }
    }
}
