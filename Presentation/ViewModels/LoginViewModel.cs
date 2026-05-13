using System;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _auth = ServiceContainer.GetService<IAuthService>();
        private readonly IDialogService _dialogService;
        private string _username;
        private string _errorMessage;

        public LoginViewModel(IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? new CarPartsShopWPF.Presentation.Services.DialogService();
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public void Login(string password)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "الرجاء إدخال اسم المستخدم";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "الرجاء إدخال كلمة المرور";
                return;
            }

            try
            {
                var user = _auth.Login(Username, password);
                if (user != null)
                {
                    _dialogService.ShowMainWindow();
                }
                else
                {
                    ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ في النظام", $"حدث خطأ أثناء تسجيل الدخول:\n{ex.Message}");
            }
        }
    }
}
