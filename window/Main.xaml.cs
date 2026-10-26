using System.Windows;
using System.Windows.Controls;
using shop_pc.pages;
using shop_pc.services;

namespace shop_pc.window {
    public partial class Main : Window {
        public Main() {
            InitializeComponent();

            var user = AuthService.CurrentUser;
            UserInfo.Text = user.FullName + "\n" + user.Role.RoleName;

            SalesButton.Visibility   = AuthService.CanSell  ? Visibility.Visible : Visibility.Collapsed;
            ReportsButton.Visibility = AuthService.IsAdmin  ? Visibility.Visible : Visibility.Collapsed;
            UsersButton.Visibility   = AuthService.IsAdmin  ? Visibility.Visible : Visibility.Collapsed;

            OpenPage("Каталог");
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            OpenPage(button.Content.ToString());
        }

        private void OpenPage(string title) {
            PageTitle.Text = title;
            switch (title) {
                case "Каталог":
                    PageHost.Content = new CatalogPage();
                    break;
                case "Продажи":
                    PageHost.Content = new SalesPage();
                    break;
                case "Сотрудники":
                    PageHost.Content = new UsersPage();
                    break;
                default:
                    PageHost.Content = new TextBlock {
                        Text = "Раздел в разработке",
                        Foreground = System.Windows.Media.Brushes.Gray
                    };
                    break;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            AuthService.Logout();
            new Authorization().Show();
            Close();
        }
    }
}