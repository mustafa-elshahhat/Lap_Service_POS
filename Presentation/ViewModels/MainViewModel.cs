using System;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _auth = ServiceContainer.GetService<IAuthService>();
        private readonly IDialogService _dialogService;
        private object _currentPage;

        public MainViewModel(IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? new CarPartsShopWPF.Presentation.Services.DialogService();
            UpdateUserInfo();
        }

        public string UserName => _auth.GetUserName();
        public string UserRole => _auth.IsAdmin ? "مدير" : "موظف";
        public bool IsAdmin => _auth.IsAdmin;
        public string CurrentDate => System.DateTime.Now.ToString("yyyy/MM/dd");

        private string _pageTitle;
        private string _pageIcon;
        private string _activeNavTag;

        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public string PageIcon
        {
            get => _pageIcon;
            set
            {
                _pageIcon = value;
                OnPropertyChanged();
            }
        }

        public string ActiveNavTag
        {
            get => _activeNavTag;
            set
            {
                _activeNavTag = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.Input.ICommand NavigateCommand => new RelayCommand<string>(Navigate);
        public System.Windows.Input.ICommand LogoutCommand => new RelayCommand(Logout);

        public Action CloseAction { get; set; }

        public void Initialize()
        {
             Navigate("POS");
        }

        private void Navigate(string destination)
        {
            ActiveNavTag = destination;

            switch (destination)
            {
                case "POS":
                    SetPage("🛒", "نقطة البيع", new POSPage());
                    break;
                case "Customers":
                    SetPage("👥", "العملاء", new CustomersPage());
                    break;
                case "Expenses":
                    SetPage("💸", "المصروفات", new ExpensesPage());
                    break;
                case "Invoices":
                    SetPage("🧾", "الفواتير", new InvoicesPage());
                    break;
                case "Returns":
                    SetPage("🔁", "المرتجعات", new ReturnsPage());
                    break;
                case "Inventory":
                    if (CheckAdmin()) SetPage("📦", "المخزون", new InventoryPage());
                    break;
                case "Suppliers":
                    if (CheckAdmin()) SetPage("🏢", "الموردين", new SuppliersPage());
                    break;
                case "Reports":
                     if (CheckAdmin()) SetPage("📊", "التقارير", new ReportsPage());
                    break;
                case "Users":
                     if (CheckAdmin()) SetPage("👤", "المستخدمين", new UsersPage());
                    break;
                case "Settings":
                     if (CheckAdmin()) SetPage("⚙️", "الإعدادات", new SettingsPage());
                    break;
            }
        }

        private void SetPage(string icon, string title, object page)
        {
            PageIcon = icon;
            PageTitle = title;
            CurrentPage = page;
        }

        private bool CheckAdmin()
        {
            if (!_auth.IsAdmin)
            {
                _dialogService.ShowError("خطأ", "ليس لديك صلاحية الوصول لهذه الصفحة");
                return false;
            }
            return true;
        }

        private void Logout()
        {
            if (_dialogService.Confirm("تسجيل الخروج", "هل تريد تسجيل الخروج؟"))
            {
                _auth.Logout();
                _dialogService.ShowLoginWindow();
                CloseAction?.Invoke();
            }
        }

        private void UpdateUserInfo()
        {
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserRole));
            OnPropertyChanged(nameof(IsAdmin));
        }
    }
}
