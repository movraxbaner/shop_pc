using System.Windows;
using shop_pc.db;
using shop_pc.viewmodels;

namespace shop_pc.window
{
    public partial class WriteOffWindow : Window
    {
        public WriteOffWindow(Product product)
        {
            InitializeComponent();
            var vm = new WriteOffViewModel(product);
            vm.CloseRequested += ok => DialogResult = ok;
            DataContext = vm;
        }
    }
}