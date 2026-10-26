using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;
using shop_pc.services;

namespace shop_pc.viewmodels
{
    public class UserEditViewModel : ViewModelBase
    {
        private const int MinPasswordLength = 6;

        private readonly int _userId;
        private string _errorMessage = "";

        public string Login { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public Role SelectedRole { get; set; }
        public bool IsActive { get; set; }

        public List<Role> Roles { get; private set; }
        public string WindowTitle { get; private set; }
        public string PasswordLabel { get; private set; }
        // свою роль и статус менять нельзя, иначе админ может случайно запереть себя
        public bool CanChangeRoleAndStatus { get; private set; }

        public ICommand SaveCommand { get; private set; }
        public event Action<bool> CloseRequested;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetField(ref _errorMessage, value); }
        }

        // user == null: новый сотрудник
        public UserEditViewModel(User user)
        {
            using (var db = new shop_pcEntities())
            {
                Roles = db.Roles.OrderBy(r => r.Id).ToList();
            }

            Password = "";

            if (user == null)
            {
                WindowTitle = "Новый сотрудник";
                PasswordLabel = "Пароль (минимум " + MinPasswordLength + " символов)";
                Login = "";
                FullName = "";
                IsActive = true;
                CanChangeRoleAndStatus = true;
            }
            else
            {
                _userId = user.Id;
                WindowTitle = "Редактирование сотрудника";
                PasswordLabel = "Новый пароль (оставьте пустым, чтобы не менять)";
                Login = user.Login;
                FullName = user.FullName;
                IsActive = user.IsActive;
                SelectedRole = Roles.FirstOrDefault(r => r.Id == user.RoleId);
                CanChangeRoleAndStatus = user.Id != AuthService.CurrentUser.Id;
            }

            SaveCommand = new RelayCommand(p => Save());
        }

        private void Save()
        {
            ErrorMessage = "";
            if (!AuthService.IsAdmin) return;

            string login = (Login ?? "").Trim();
            string fullName = (FullName ?? "").Trim();
            string password = Password ?? "";

            if (login == "") { ErrorMessage = "Введите логин"; return; }
            if (login.Length > 50 || login.Contains(" "))
            {
                ErrorMessage = "Логин: до 50 символов, без пробелов";
                return;
            }
            if (fullName == "") { ErrorMessage = "Введите ФИО"; return; }
            if (fullName.Length > 150) { ErrorMessage = "ФИО слишком длинное"; return; }
            if (SelectedRole == null) { ErrorMessage = "Выберите роль"; return; }

            bool isNew = _userId == 0;
            if (isNew && password == "") { ErrorMessage = "Введите пароль"; return; }
            if (password != "" && password.Length < MinPasswordLength)
            {
                ErrorMessage = "Пароль должен быть не короче " + MinPasswordLength + " символов";
                return;
            }

            try
            {
                using (var db = new shop_pcEntities())
                {
                    if (db.Users.Any(u => u.Login == login && u.Id != _userId))
                    {
                        ErrorMessage = "Сотрудник с таким логином уже есть";
                        return;
                    }

                    User user;
                    if (isNew)
                    {
                        user = new User();
                        db.Users.Add(user);
                    }
                    else
                    {
                        user = db.Users.Find(_userId);
                        if (user == null) { ErrorMessage = "Сотрудник не найден"; return; }
                    }

                    user.Login = login;
                    user.FullName = fullName;
                    if (CanChangeRoleAndStatus)
                    {
                        user.RoleId = SelectedRole.Id;
                        user.IsActive = IsActive;
                    }
                    if (password != "")
                    {
                        user.PasswordHash = AuthService.Hash(password);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Не удалось сохранить: " + ex.Message;
                return;
            }

            CloseRequested?.Invoke(true);
        }
    }
}