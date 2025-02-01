// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/CommunityToolkit/Windows/blob/main/components/Behaviors/src/NavigateToUriAction.cs

using System;
using DocumentFormat.OpenXml.Bibliography;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace LogoSlideMaker.WinUi.Pickers;

/// <summary>
/// NavigateToUriAction represents an action that allows navigate to a specified URL defined in XAML, similiar to a Hyperlink and HyperlinkButton. No action will be invoked if the Uri cannot be navigated to.
/// </summary>
public sealed partial class ShowFileSavePickerAction : DependencyObject, IAction
{
    /// <summary>
    /// Gets or sets the Uniform Resource Identifier (URI) to navigate to when the object is clicked.
    /// </summary>
    public FileSavePickerViewModel Source
    {
        get => (FileSavePickerViewModel)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="NavigateUri"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(FileSavePickerViewModel),
        typeof(ShowFileSavePickerAction),
        new PropertyMetadata(null));

    /// <inheritdoc/>
    public object Execute(object sender, object parameter)
    {
        if (Source != null)
        {
            Console.WriteLine(Source.SuggestedFileName);
        }
        else
        {
            throw new ArgumentNullException(nameof(Source));
        }

        return true;
    }
}
