using System.Windows;
using shop_pc.db;
using shop_pc.viewmodels;

namespace shop_pc.window
{
    public partial class ProductEditWindow : Window
    {
        public ProductEditWindow(Product product)
        {
            InitializeComponent();
            var vm = new ProductEditViewModel(product);
            vm.CloseRequested += ok => DialogResult = ok;   
            DataContext = vm;
        }
    }
}