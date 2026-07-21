using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia;
using System.Threading.Tasks;

namespace HotspotMaker.Util.UI
{
    public static class WindowExtensions
    {
        /// <summary>
        /// Shows this window as a dialog. Uses the main window as owner if no owner is provided. The window is placed at the center of its owner.
        /// </summary>
        public static async Task<bool> ShowAsDialog(this Window window, Window? owner = null)
        {
            if (owner == null)
            {
                owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner == null)
                    return false;
            }

            window.Position = new PixelPoint(
                owner.Position.X + (int)((owner.Width - window.Width) / 2),
                owner.Position.Y + (int)((owner.Height - window.Height) / 2));

            await window.ShowDialog(owner);
            return true;
        }
    }
}
