using ShapeCrawler;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;

namespace LogoSlideMaker.Render;

/// <summary>
/// Render a logo layout to presentation slide shapes
/// </summary>
public class Renderer(RenderConfig config)
{
    public void Render(ILayout layout, ISlideShapes shapes)
    {
        if (config.Listing)
        {   
            Console.WriteLine();
            Console.WriteLine($"## {layout.Name}");
            Console.WriteLine();
            foreach(var line in layout.Description)
            {
                Console.WriteLine(line);        
            }
        }

        // Fill in description field
        if (layout.Description.Count() > 0)
        {
            var description_box = shapes.TryGetByName<IShape>("Description");
            if (description_box is not null)
            {
                var tf = description_box.TextFrame;
                var maxlines = Math.Min(layout.Description.Count(),tf.Paragraphs.Count);
                for (int l = 0; l < maxlines; l++)
                {
                    tf.Paragraphs[l].Text = layout.Description.Skip(l).First();
                }
            }
        }

        foreach(var boxlayout in layout)
        {
            if (config.Listing)
            {   
                Console.WriteLine();
                Console.WriteLine($"### {boxlayout.Heading}");
                Console.WriteLine();
            }

            foreach(var logolayout in boxlayout.Logos)
            {                
                var logo = logolayout.Logo;

                if (logo is null)
                {
                    continue;
                }

                if (config.Listing)
                {
                    string alt_text = string.IsNullOrWhiteSpace(logo.AltText) ? string.Empty : $"{logo.AltText} ";
                    Console.WriteLine($"* {alt_text}{logo.Title}");
                }

                {
                    using var stream = new FileStream(logo.Path,FileMode.Open);
                    shapes.AddPicture(stream);
                }
                
                var pic = shapes.OfType<IPicture>().Last();

                // Adjust size of icon depending on size of source image. The idea is all
                // icons occupy the same number of pixel area

                var aspect = pic.Width / pic.Height;
                var width_factor = (decimal)Math.Sqrt((double)aspect);
                var height_factor = 1.0m / width_factor;
                var icon_width = config.IconSize * width_factor * logo.Scale;
                var icon_height = config.IconSize * height_factor * logo.Scale;

                pic.X = ( logolayout.X - icon_width / 2.0m ) * config.Dpi;
                pic.Y = ( logolayout.Y - icon_height / 2.0m ) * config.Dpi;
                pic.Width = icon_width * config.Dpi;
                pic.Height = icon_height * config.Dpi;

                var text_width_inches = logo.TextWidth ?? config.TextWidth;

                decimal text_x = ( logolayout.X - text_width_inches / 2.0m ) * config.Dpi;
                decimal text_y = ( logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi;
                decimal text_width = text_width_inches * config.Dpi;
                decimal text_height = config.TextHeight * config.Dpi;

                shapes.AddRectangle(100,100,100,100);
                var shape = shapes.Last();

                shape.X = text_x;
                shape.Y = text_y;
                shape.Width = text_width;
                shape.Height = text_height;

                var tf = shape.TextFrame;
                tf.Text = logo.Title;
                tf.LeftMargin = 0;
                tf.RightMargin = 0;
                var font = tf.Paragraphs.First().Portions.First().Font;

                font.Size = config.FontSize;
                font.LatinName = config.FontName;
                font.Color.Update(config.FontColor);
                shape.Fill.SetNoFill(); //SetColor(config.BackgroundColor);
                shape.Outline.Weight = 0;
                shape.Outline.HexColor = config.BackgroundColor;            
            }
        }
    }
}
