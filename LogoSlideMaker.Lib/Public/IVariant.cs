﻿using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Public;

/// <summary>
/// Defines a single slide to be output
/// </summary>
public interface IVariant
{
    /// <summary>
    /// The descriptive name of this slide
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The detailed description of what's on this slide
    /// </summary>
    ICollection<string> Description { get; }

    /// <summary>
    /// Additional notes which should be included in the slide notes
    /// </summary>
    ICollection<string> Notes { get; }

    /// <summary>
    /// Title of the overall document
    /// </summary>
    string? DocumentTitle { get; }

    /// <summary>
    /// The slide number upon which this page should be placed
    /// </summary>
    int Source { get; }

    /// <summary>
    /// The styles which should be used to render text on this slide
    /// </summary>
    IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles { get; }

    /// <summary>
    /// Index of this slide within the larger definition, 0-based
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Generate the primitives needed to render this slide
    /// </summary>
    /// <param name="bitmaps"></param>
    /// <returns></returns>
    ICollection<Primitive> GeneratePrimitives(IGetImageAspectRatio bitmaps);
}
