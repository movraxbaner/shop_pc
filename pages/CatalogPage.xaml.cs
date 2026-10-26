using System.Windows.Controls;
using shop_pc.viewmodels;

namespace shop_pc.pages
{
    public partial class CatalogPage : UserControl
    {
        public CatalogPage()
        {
            InitializeComponent();
            DataContext = new CatalogViewModel();
        }
    }
}