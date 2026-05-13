using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class UsersViewModel : BaseViewModel
    {
        private readonly IAuthService _auth;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Dictionary<string, object>> _users;
        private Dictionary<string, object> _selectedUser;

        public UsersViewModel(IDialogService dialogService = null)
        {
            _auth = ServiceContainer.GetService<IAuthService>();
            _dialogService = dialogService ?? new AlJohary.ServiceHub.Presentation.Services.DialogService();
            LoadUsers();
        }

        public ObservableCollection<Dictionary<string, object>> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, object> SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand AddUserCommand => new RelayCommand(AddUser);
        public ICommand EditUserCommand => new RelayCommand(EditUser, () => SelectedUser != null);
        public ICommand ChangePasswordCommand => new RelayCommand(ChangePassword, () => SelectedUser != null);
        public ICommand DeleteUserCommand => new RelayCommand(DeleteUser, () => SelectedUser != null);

        private void LoadUsers()
        {
            try
            {
                var usersList = _auth.GetAllUsers();
                Users = new ObservableCollection<Dictionary<string, object>>(usersList ?? new List<Dictionary<string, object>>());
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void AddUser()
        {
            try
            {
                if (!_auth.IsAdmin)
                {
                    _dialogService.ShowWarning("تنبيه", "ليس لديك صلاحية إضافة مستخدمين");
                    return;
                }

                var vm = new UserFormViewModel(false);

                if (_dialogService.ShowUserFormDialog(vm) == true)
                {
                    try
                    {

                        _auth.CreateUser(vm.Username, vm.Password, vm.FullName, vm.Role,
                            vm.MaxDiscountPercent, vm.MaxMarkupPercent);
                        _dialogService.ShowSuccess("نجاح", "تم إنشاء المستخدم بنجاح");
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowError("خطأ", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void EditUser()
        {
            if (SelectedUser == null) return;

            try
            {
                int userId = SafeConvert.ToInt(SelectedUser["id"]);
                string username = SafeConvert.ToString(SelectedUser["username"]);
                string fullName = SafeConvert.ToString(SelectedUser["full_name"]);
                string role = SafeConvert.ToString(SelectedUser["role"]);
                double maxDiscount = SafeConvert.ToDouble(SelectedUser["max_discount_percent"]);
                double maxMarkup = SafeConvert.ToDouble(SelectedUser["max_markup_percent"]);

                var vm = new UserFormViewModel(true)
                {
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    MaxDiscountPercent = maxDiscount,
                    MaxMarkupPercent = maxMarkup
                };

                if (_dialogService.ShowUserFormDialog(vm) == true)
                {

                    _auth.UpdateUser(userId, vm.FullName, vm.Role, vm.MaxDiscountPercent, vm.MaxMarkupPercent);

                    if (!string.IsNullOrEmpty(vm.Password))
                    {
                        _auth.ChangeUserPassword(userId, vm.Password);
                    }

                    _dialogService.ShowSuccess("نجاح", "تم تحديث بيانات المستخدم");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void ChangePassword()
        {
            if (SelectedUser == null) return;

            int userId = SafeConvert.ToInt(SelectedUser["id"]);

            if (_dialogService.ShowInputDialog("تغيير كلمة المرور", "كلمة المرور الجديدة:", "", out string newPassword) == true)
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _dialogService.ShowWarning("تنبيه", "الرجاء إدخال كلمة مرور صحيحة");
                    return;
                }

                try
                {
                    _auth.ChangeUserPassword(userId, newPassword);
                    _dialogService.ShowSuccess("نجاح", "تم تغيير كلمة المرور بنجاح");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
        }

        private void DeleteUser()
        {
            if (SelectedUser == null) return;

            int userId = SafeConvert.ToInt(SelectedUser["id"]);
            string userName = SafeConvert.ToString(SelectedUser["full_name"]);

            if (_dialogService.Confirm("تأكيد الحذف", $"هل تريد حذف المستخدم:\n{userName}؟"))
            {
                try
                {
                    _auth.DeleteUser(userId);
                    _dialogService.ShowSuccess("نجاح", "تم حذف المستخدم بنجاح");
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
        }
    }
}
