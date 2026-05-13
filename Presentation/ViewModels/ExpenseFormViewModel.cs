using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class ExpenseFormViewModel : BaseViewModel
    {
        private readonly IExpenseService _expenseService;
        private string _description;
        private decimal _amount;
        private string _category;
        private string _paymentMethod;
        private ObservableCollection<string> _categories;
        private ObservableCollection<string> _paymentMethods;

        public ExpenseFormViewModel()
        {
            _expenseService = ServiceContainer.GetService<IExpenseService>();
            Categories = new ObservableCollection<string>();
            PaymentMethodsList = new ObservableCollection<string>(PaymentMethods.GetAll());
            PaymentMethod = PaymentMethods.Cash;
            _category = "أخرى";
            LoadCategories();
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<string> PaymentMethodsList
        {
            get => _paymentMethods;
            set => SetProperty(ref _paymentMethods, value);
        }

        private void LoadCategories()
        {
            try
            {
                var dbCategories = _expenseService.GetCategories();

                var uniqueList = new HashSet<string>
                {
                    "رواتب", "إيجار", "كهرباء", "أخرى", "بضاعة", "صيانة", "نقل", "ضيافة"
                };

                foreach(var c in dbCategories)
                {
                    if(!string.IsNullOrEmpty(c)) uniqueList.Add(c.Trim());
                }

                var list = uniqueList.ToList();
                list.Sort();

                Categories.Clear();
                foreach (var item in list)
                {
                    Categories.Add(item);
                }
            }
            catch { }
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(Description))
            {
                errorMessage = "الرجاء إدخال وصف المصروف";
                return false;
            }

            if (Amount <= 0)
            {
                errorMessage = "الرجاء إدخال مبلغ صحيح";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Category))
            {
                errorMessage = "الرجاء إدخال نوع المصروف";
                return false;
            }

            return true;
        }
    }
}
