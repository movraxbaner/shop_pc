using System.Windows.Controls;
using shop_pc.viewmodels;

namespace shop_pc.pages
{
    public partial class UsersPage : UserControl
    {
        public UsersPage()
        {
            InitializeComponent();
            DataContext = new UsersViewModel();
        }
    }
}