using System.Windows.Controls;
using shop_pc.viewmodels;

namespace shop_pc.pages
{
    public partial class SalesPage : UserControl
    {
        public SalesPage()
        {
            InitializeComponent();
            DataContext = new SalesViewModel();
        }
    }
}