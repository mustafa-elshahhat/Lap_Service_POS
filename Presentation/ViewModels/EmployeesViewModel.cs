using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class EmployeesViewModel : BaseViewModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Employee> _employees;
        private Employee _selectedEmployee;
        private string _searchText;

        public EmployeesViewModel(IDialogService dialogService = null)
        {
            _employeeService = ServiceContainer.GetService<IEmployeeService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            Employees = new ObservableCollection<Employee>();
            LoadEmployees();
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (SetProperty(ref _selectedEmployee, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    LoadEmployees();
            }
        }

        public ICommand LoadAllCommand => new RelayCommand(LoadAll);
        public ICommand AddEmployeeCommand => new RelayCommand(AddEmployee);
        public ICommand EditEmployeeCommand => new RelayCommand(EditEmployee, () => SelectedEmployee != null);
        public ICommand ToggleActiveCommand => new RelayCommand(ToggleActive, () => SelectedEmployee != null);
        public ICommand RegisterSalaryCommand => new RelayCommand(RegisterSalary, () => SelectedEmployee != null);
        public ICommand RegisterDeductionCommand => new RelayCommand(RegisterDeduction, () => SelectedEmployee != null);

        private void LoadAll()
        {
            SearchText = string.Empty;
            LoadEmployees();
            OnRequestSearchFocus();
        }

        private void LoadEmployees()
        {
            try
            {
                var list = _employeeService.GetAllEmployees(true) ?? new List<Employee>();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string q = SearchText.Trim().ToLower();
                    list = list.Where(e =>
                        SafeConvert.ToString(e.FullName).ToLower().Contains(q) ||
                        SafeConvert.ToString(e.Phone).ToLower().Contains(q) ||
                        SafeConvert.ToString(e.JobTitle).ToLower().Contains(q)).ToList();
                }

                Employees = new ObservableCollection<Employee>(list);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void AddEmployee()
        {
            try
            {
                var vm = new EmployeeFormViewModel(false);
                if (_dialogService.ShowEmployeeFormDialog(vm) == true)
                {
                    _employeeService.CreateEmployee(vm.FullName, vm.Phone, vm.JobTitle, vm.BaseSalary, vm.Notes);
                    _dialogService.ShowSuccess("نجاح", "تم إضافة الموظف بنجاح");
                    LoadEmployees();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
            OnRequestSearchFocus();
        }

        private void EditEmployee()
        {
            if (SelectedEmployee == null) return;

            try
            {
                var vm = new EmployeeFormViewModel(true)
                {
                    FullName = SelectedEmployee.FullName,
                    Phone = SelectedEmployee.Phone,
                    JobTitle = SelectedEmployee.JobTitle,
                    BaseSalary = SelectedEmployee.BaseSalary,
                    Notes = SelectedEmployee.Notes
                };

                if (_dialogService.ShowEmployeeFormDialog(vm) == true)
                {
                    _employeeService.UpdateEmployee(SelectedEmployee.Id, vm.FullName, vm.Phone, vm.JobTitle, vm.BaseSalary, vm.Notes);
                    _dialogService.ShowSuccess("نجاح", "تم تعديل بيانات الموظف بنجاح");
                    LoadEmployees();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
            OnRequestSearchFocus();
        }

        private void ToggleActive()
        {
            if (SelectedEmployee == null) return;

            string action = SelectedEmployee.IsActive ? "تعطيل" : "تفعيل";
            if (!_dialogService.Confirm("تأكيد", $"هل تريد {action} الموظف:\n{SelectedEmployee.FullName}؟"))
                return;

            try
            {
                _employeeService.SetEmployeeActive(SelectedEmployee.Id, !SelectedEmployee.IsActive);
                LoadEmployees();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
            OnRequestSearchFocus();
        }

        private void RegisterSalary()
        {
            RegisterTransaction("salary");
        }

        private void RegisterDeduction()
        {
            RegisterTransaction("deduction");
        }

        private void RegisterTransaction(string transactionType)
        {
            if (SelectedEmployee == null) return;

            if (!SelectedEmployee.IsActive)
            {
                _dialogService.ShowWarning("تنبيه", "لا يمكن تسجيل راتب أو خصم لموظف غير نشط");
                return;
            }

            if (_dialogService.ShowEmployeeSalaryTransactionDialog(SelectedEmployee, transactionType,
                out decimal amount, out string paymentMethod, out DateTime transactionDate, out string notes) == true)
            {
                try
                {
                    if (transactionType == "salary")
                    {
                        _employeeService.RegisterSalaryPayment(SelectedEmployee.Id, amount, paymentMethod, transactionDate, notes);
                        _dialogService.ShowSuccess("نجاح", "تم تسجيل صرف الراتب بنجاح");
                    }
                    else
                    {
                        _employeeService.RegisterDeduction(SelectedEmployee.Id, amount, transactionDate, notes);
                        _dialogService.ShowSuccess("نجاح", "تم تسجيل الخصم بنجاح");
                    }

                    TypedMessenger.Send("RefreshReports");
                    LoadEmployees();
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            OnRequestSearchFocus();
        }
    }
}
