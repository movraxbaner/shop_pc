using shop_pc.services;
using System;
using System.Windows;

namespace shop_pc.window {
    public partial class Authorization : Window {
        public Authorization() {
            InitializeComponent();
        }

        private void LoginButton(object sender, RoutedEventArgs e) {
            string login = LoginBox.Text.Trim();
            string password = PasswordInput.Password;

            if (login == "" || password == "")
            {
                MessageBox.Show("Заполните все данные", "Ошибка");
                return;
            }

            bool ok;
            try
            {
                ok = AuthService.Login(login, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подключиться к базе данных:\n" + ex.Message, "Ошибка");
                return;
            }

            if (!ok)
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка");
                return;
            }

            new Main().Show();
            Close();
        }


#if false
        private void TestLogin(string l, string p) {
            if (string.IsNullOrWhiteSpace(l) || string.IsNullOrWhiteSpace(p)) {
                MessageBox.Show("Заполните все данные", "Ошибка");
                return;
            }
            if (l == "admin" && p == "123") {
                Main main = new Main();
                main.Show();
                Close();
            }
            else {
                MessageBox.Show("Неверный логин или пароль", "Ошибка");
            }
        }
#endif

    }
}
