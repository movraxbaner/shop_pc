using System.Linq;
using System.Windows;

namespace shop_pc.helpers
{
    public static class DialogHelper
    {
        public static bool ShowDialog(Window window)
        {
            Window owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (owner != null && owner != window) window.Owner = owner;
            return window.ShowDialog() == true;
        }
    }
}