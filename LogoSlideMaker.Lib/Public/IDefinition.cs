﻿namespace LogoSlideMaker.Public;

/// <summary>
/// Defines a document containing multiple slides to be output to a PowerPoint presentation
/// </summary>
public interface IDefinition
{
    /// <summary>
    /// The individual slide to be output
    /// </summary>
    IList<IVariant> Variants { get; }

    /// <summary>
    /// Paths for all the images we will need to render
    /// </summary>
    ICollection<string> ImagePaths { get; }

    /// <summary>
    /// Name of the presentation file where we will save to
    /// </summary>
    string? OutputFileName { get; }

    /// <summary>
    /// Name of the presentation file containing the base template
    /// </summary>
    string? TemplateSlidesFileName { get; }

    /// <summary>
    /// True if the user would like to see a textual listing 
    /// </summary>
    bool Listing { get; }

    /// <summary>
    /// Render the slides in textual listing form
    /// </summary>
    /// <param name="output"></param>
    void RenderListing(TextWriter output);
}
