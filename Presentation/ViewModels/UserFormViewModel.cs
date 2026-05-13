using System;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class UserFormViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private string _username;
        private string _fullName;
        private string _password;
        private string _role;
        private bool _isEditMode;
        private string _title;
        private string _buttonText;

        public Action<bool> CloseAction { get; set; }

        public UserFormViewModel(bool isEditMode = false, IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? new CarPartsShopWPF.Presentation.Services.DialogService();
            IsEditMode = isEditMode;
            Title = isEditMode ? "تعديل مستخدم" : "إضافة مستخدم";
            ButtonText = isEditMode ? "حفظ التعديلات" : "إنشاء";
            Role = "employee";
        }

        #region Properties

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        private double _maxDiscountPercent;
        public double MaxDiscountPercent
        {
            get => _maxDiscountPercent;
            set => SetProperty(ref _maxDiscountPercent, value);
        }

        private double _maxMarkupPercent;
        public double MaxMarkupPercent
        {
            get => _maxMarkupPercent;
            set => SetProperty(ref _maxMarkupPercent, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand CancelCommand => new RelayCommand(Cancel);

        #endregion

        #region Methods

        private void Save()
        {

            if (string.IsNullOrWhiteSpace(Username))
            {
                _dialogService.ShowWarning("تنبيه", "الرجاء إدخال اسم مستخدم صالح");
                return;
            }

            if (!IsEditMode)
            {
                if (string.IsNullOrWhiteSpace(Password) || Password.Length < 4)
                {
                    _dialogService.ShowWarning("تنبيه", "الرجاء إدخال كلمة مرور صحيحة (4 أحرف على الأقل)");
                    return;
                }
            }
            else
            {

                if (!string.IsNullOrEmpty(Password) && Password.Length < 4)
                {
                    _dialogService.ShowWarning("تنبيه", "كلمة المرور قصيرة جداً (4 أحرف على الأقل)");
                    return;
                }
            }

            if (string.IsNullOrEmpty(Role))
            {
                _dialogService.ShowWarning("تنبيه", "الرجاء اختيار صلاحية المستخدم");
                return;
            }

            CloseAction?.Invoke(true);
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }

        #endregion
    }
}

