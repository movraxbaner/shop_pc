using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;
using shop_pc.services;
using shop_pc.window;

namespace shop_pc.viewmodels
{
    public class UsersViewModel : ViewModelBase
    {
        private List<User> _users = new List<User>();
        private User _selectedUser;

        public UsersViewModel()
        {
            AddCommand = new RelayCommand(p => OpenEditor(null), p => AuthService.IsAdmin);
            EditCommand = new RelayCommand(p => OpenEditor(SelectedUser),
                                             p => AuthService.IsAdmin && SelectedUser != null);
            // себя заблокировать нельзя
            ToggleActiveCommand = new RelayCommand(p => ToggleActive(),
                p => AuthService.IsAdmin && SelectedUser != null
                     && SelectedUser.Id != AuthService.CurrentUser.Id);
            Load();
        }

        public List<User> Users
        {
            get { return _users; }
            private set { SetField(ref _users, value); }
        }

        public User SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                if (SetField(ref _selectedUser, value)) OnPropertyChanged(nameof(ToggleButtonText));
            }
        }

        public string ToggleButtonText
        {
            get
            {
                return SelectedUser != null && !SelectedUser.IsActive ? "Разблокировать" : "Заблокировать";
            }
        }

        public ICommand AddCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand ToggleActiveCommand { get; private set; }

        private void Load()
        {
            using (var db = new shop_pcEntities())
            {
                Users = db.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToList();
            }
        }

        private void OpenEditor(User user)
        {
            if (DialogHelper.ShowDialog(new UserEditWindow(user))) Load();
        }

        private void ToggleActive()
        {
            if (SelectedUser == null) return;
            using (var db = new shop_pcEntities())
            {
                User user = db.Users.Find(SelectedUser.Id);
                if (user == null) return;
                user.IsActive = !user.IsActive;
                db.SaveChanges();
            }
            Load();
        }
    }
}