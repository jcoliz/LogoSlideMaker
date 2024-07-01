// Copyright (C) 2024 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using System;
using System.Runtime.InteropServices;

namespace LogoSlideMaker.WinUi;

/// <summary>
/// Manage console windows
/// </summary>
/// <remarks>
/// Manages Win32 DLL's.
/// From https://stackoverflow.com/questions/31978826/is-it-possible-to-have-a-wpf-application-print-console-output
/// </remarks>
internal static partial class ConsoleAllocator
{
    [LibraryImport(@"kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport(@"kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport(@"user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SwHide = 0;
    private const int SwShow = 5;

    public static void ShowConsoleWindow()
    {
        var handle = GetConsoleWindow();

        if (handle == IntPtr.Zero)
        {
            AllocConsole();
        }
        else
        {
            ShowWindow(handle, SwShow);
        }
    }

    public static void HideConsoleWindow()
    {
        var handle = GetConsoleWindow();

        ShowWindow(handle, SwHide);
    }
}