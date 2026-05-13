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
    public class ExpensesViewModel : BaseViewModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IAuthService _auth;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Dictionary<string, object>> _expenses;
        private Dictionary<string, object> _selectedExpense;
        private string _searchText;

        public ExpensesViewModel(IDialogService dialogService = null)
        {
            _expenseService = ServiceContainer.GetService<IExpenseService>();
            _auth = ServiceContainer.GetService<IAuthService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            LoadExpenses();
        }

        public ObservableCollection<Dictionary<string, object>> Expenses
        {
            get => _expenses;
            set
            {
                _expenses = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, object> SelectedExpense
        {
            get => _selectedExpense;
            set
            {
                _selectedExpense = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadExpenses();
            }
        }

        public ICommand LoadAllCommand => new RelayCommand(LoadAll);
        public ICommand AddExpenseCommand => new RelayCommand(AddExpense);
        public ICommand DeleteExpenseCommand => new RelayCommand(DeleteExpense, () => SelectedExpense != null);

        private void LoadAll()
        {
            SearchText = "";
            LoadExpenses();
            OnRequestSearchFocus();
        }

        private void LoadExpenses()
        {
            try
            {
                string start = "2020-01-01";
                string end = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

                var list = _expenseService.GetExpensesByDateRange(start, end);

                if (!string.IsNullOrEmpty(SearchText))
                {
                     string q = SearchText.ToLower();
                     list = list.FindAll(x =>
                        SafeConvert.ToString(x["description"]).ToLower().Contains(q) ||
                        SafeConvert.ToString(x["category"]).ToLower().Contains(q)
                     );
                }

                Expenses = new ObservableCollection<Dictionary<string, object>>(list ?? new List<Dictionary<string, object>>());
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void AddExpense()
        {
            var vm = new ExpenseFormViewModel();

            if (_dialogService.ShowExpenseDialog(vm) == true)
            {
                try
                {
                    _expenseService.CreateExpense(
                        vm.Description,
                        vm.Amount,
                        vm.Category,
                        vm.PaymentMethod,
                        DateTime.Now
                    );

                    _dialogService.ShowSuccess("نجاح", "تم إضافة المصروف بنجاح");
                    LoadExpenses();
                    TypedMessenger.Send("RefreshReports");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            OnRequestSearchFocus();
        }

        private void DeleteExpense()
        {
            if (SelectedExpense == null) return;

            if (_dialogService.Confirm("تأكيد الحذف", "هل تريد حذف هذا المصروف؟"))
            {
                try
                {
                    int id = SafeConvert.ToInt(SelectedExpense["id"]);
                    _expenseService.DeleteExpense(id);
                    LoadExpenses();
                    TypedMessenger.Send("RefreshReports");
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
