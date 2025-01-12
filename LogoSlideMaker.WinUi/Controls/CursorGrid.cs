using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace LogoSlideMaker.WinUi.Controls;

/// <summary>
/// A grid with its own cursor.
/// </summary>
/// <seealso href="https://stackoverflow.com/questions/76578003/how-can-i-change-the-pointer-of-my-cursor-in-winui3" />
internal class CursorGrid: Grid
{
    public CursorGrid()
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}
