using ShapeCrawler;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;

namespace LogoSlideMaker.Render;

/// <summary>
/// Render a logo layout to presentation slide shapes
/// </summary>
public class Renderer(RenderConfig config)
{
    public void Render(ILayout source, ISlideShapes target)
    {
        if (config.Listing)
        {   
            Console.WriteLine();
            Console.WriteLine($"## {source.Name}");
            Console.WriteLine();
            foreach(var line in source.Description)
            {
                Console.WriteLine(line);        
            }
        }

        // Fill in description field
        var num_description_lines = source.Description.Count();
        if (num_description_lines > 0)
        {
            var description_box = target.TryGetByName<IShape>("Description");
            if (description_box is not null)
            {
                var tf = description_box.TextFrame;

                // Ensure there are enough paragraphs to insert text
                while (tf.Paragraphs.Count < num_description_lines)
                {
                    tf.Paragraphs.Add();
                }

                var queue = new Queue<string>(source.Description);
                foreach(var para in tf.Paragraphs)
                {
                    if (queue.Count == 0)
                    {
                        break;
                    }
                    para.Text = queue.Dequeue();
                }
            }
        }

        foreach(var boxlayout in source)
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
                    target.AddPicture(stream);
                }
                
                var pic = target.OfType<IPicture>().Last();

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

                target.AddRectangle(100,100,100,100);
                var shape = target.Last();

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
                shape.Fill.SetNoFill();
                shape.Outline.SetNoOutline();            
            }
        }
    }
}
