using System.Windows;
using shop_pc.db;
using shop_pc.viewmodels;

namespace shop_pc.window
{
    public partial class UserEditWindow : Window
    {
        private readonly UserEditViewModel _vm;

        public UserEditWindow(User user)
        {
            InitializeComponent();
            _vm = new UserEditViewModel(user);
            _vm.CloseRequested += ok => DialogResult = ok;
            DataContext = _vm;
        }

        // У PasswordBox нет привязки Binding (так сделано специально из соображений
        // безопасности), поэтому пароль передаём во ViewModel вручную.
        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _vm.Password = PasswordInput.Password;
        }
    }
}