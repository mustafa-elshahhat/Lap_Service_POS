using System;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _auth = ServiceContainer.GetService<IAuthService>();
        private readonly IDialogService _dialogService;
        private string _username;
        private string _errorMessage;

        public LoginViewModel(IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? new AlJohary.ServiceHub.Presentation.Services.DialogService();
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
                    if (_auth.IsForcePasswordChangeRequired())
                    {
                        if (!ForcePasswordChange())
                        {
                            return;
                        }
                    }
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
        private bool ForcePasswordChange()
        {
            while (true)
            {
                // TODO: mask password input when a masked dialog is available.
                if (_dialogService.ShowInputDialog("تغيير كلمة المرور الإجبارية",
                    "يجب تغيير كلمة المرور الافتراضية. أدخل كلمة المرور الجديدة:", "", out string newPassword) != true)
                {
                    _auth.Logout();
                    ErrorMessage = "يجب تغيير كلمة المرور الافتراضية قبل المتابعة";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                {
                    _dialogService.ShowWarning("تنبيه", "كلمة المرور يجب أن تكون على الأقل 4 أحرف");
                    continue;
                }

                if (_dialogService.ShowInputDialog("تأكيد كلمة المرور",
                    "أعد إدخال كلمة المرور الجديدة:", "", out string confirmPassword) != true)
                {
                    _auth.Logout();
                    ErrorMessage = "يجب تأكيد كلمة المرور الجديدة";
                    return false;
                }

                if (newPassword != confirmPassword)
                {
                    _dialogService.ShowWarning("تنبيه", "كلمة المرور غير متطابقة");
                    continue;
                }

                try
                {
                    _auth.ChangeUserPassword(_auth.GetUserId(), newPassword);
                    _auth.ClearForcePasswordChangeFlag();
                    _dialogService.ShowSuccess("نجاح", "تم تغيير كلمة المرور بنجاح");
                    return true;
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                    return false;
                }
            }
        }
    }
}
