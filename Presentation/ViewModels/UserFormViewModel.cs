using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class EmployeeOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }

        // Safety net: if any control ever displays this object without a
        // DisplayMemberPath/ItemTemplate, fall back to the readable name
        // instead of the fully-qualified type name.
        public override string ToString() => DisplayName ?? string.Empty;
    }

    public class UserFormViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private string _username;
        private string _fullName;
        private string _password;
        private string _role;
        private ObservableCollection<EmployeeOption> _employeeOptions;
        private int _selectedEmployeeId;
        private bool _isEditMode;
        private string _title;
        private string _buttonText;

        public Action<bool> CloseAction { get; set; }

        public UserFormViewModel(bool isEditMode = false, IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? new AlJohary.ServiceHub.Presentation.Services.DialogService();
            IsEditMode = isEditMode;
            Title = isEditMode ? "تعديل مستخدم" : "إضافة مستخدم";
            ButtonText = isEditMode ? "حفظ التعديلات" : "إنشاء";
            Role = "employee";
            EmployeeOptions = new ObservableCollection<EmployeeOption>();
            LoadEmployees();
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
            set
            {
                if (SetProperty(ref _role, value))
                {
                    OnPropertyChanged(nameof(IsAdminRole));
                    OnPropertyChanged(nameof(DiscountFieldsEnabled));
                }
            }
        }

        public bool IsAdminRole => Role == "admin";
        public bool DiscountFieldsEnabled => !IsAdminRole;

        public ObservableCollection<EmployeeOption> EmployeeOptions
        {
            get => _employeeOptions;
            set => SetProperty(ref _employeeOptions, value);
        }

        public int SelectedEmployeeId
        {
            get => _selectedEmployeeId;
            set => SetProperty(ref _selectedEmployeeId, value);
        }

        public int? EmployeeId => SelectedEmployeeId > 0 ? SelectedEmployeeId : (int?)null;

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

        public void LoadEmployees(int? currentEmployeeId = null, string currentEmployeeName = null)
        {
            EmployeeOptions.Clear();
            EmployeeOptions.Add(new EmployeeOption { Id = 0, DisplayName = "بدون ربط" });

            try
            {
                var employeeService = ServiceContainer.GetService<IEmployeeService>();
                var employees = employeeService.GetActiveEmployees();
                foreach (var employee in employees)
                {
                    EmployeeOptions.Add(new EmployeeOption { Id = employee.Id, DisplayName = employee.FullName });
                }

                if (currentEmployeeId.HasValue && currentEmployeeId.Value > 0 && !EmployeeOptions.Any(e => e.Id == currentEmployeeId.Value))
                {
                    var employee = employeeService.GetById(currentEmployeeId.Value);
                    if (employee != null)
                    {
                        string suffix = employee.IsActive ? string.Empty : " (غير نشط)";
                        EmployeeOptions.Add(new EmployeeOption { Id = employee.Id, DisplayName = employee.FullName + suffix });
                    }
                    else if (!string.IsNullOrWhiteSpace(currentEmployeeName) && currentEmployeeName != "-")
                    {
                        EmployeeOptions.Add(new EmployeeOption { Id = currentEmployeeId.Value, DisplayName = currentEmployeeName });
                    }
                }
            }
            catch
            {
                if (currentEmployeeId.HasValue && currentEmployeeId.Value > 0 && !string.IsNullOrWhiteSpace(currentEmployeeName) && currentEmployeeName != "-")
                    EmployeeOptions.Add(new EmployeeOption { Id = currentEmployeeId.Value, DisplayName = currentEmployeeName });
            }

            SelectedEmployeeId = currentEmployeeId ?? 0;
        }

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

            if (!IsAdminRole && (MaxDiscountPercent < 0 || MaxDiscountPercent > 100 || MaxMarkupPercent < 0 || MaxMarkupPercent > 100))
            {
                _dialogService.ShowWarning("تنبيه", "نسب الخصم والزيادة للموظف يجب أن تكون بين 0 و 100");
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
