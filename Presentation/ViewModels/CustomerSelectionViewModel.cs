using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CustomerSelectionViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly IDialogService _dialogService;
        public Action<Customer> OnCustomerSelected { get; set; }
        public Action CloseAction { get; set; }

        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private string _searchText;

        public CustomerSelectionViewModel(IDialogService dialogService = null)
        {
            _customerService = ServiceContainer.GetService<ICustomerService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            Customers = new ObservableCollection<Customer>();
            LoadCustomers();
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PerformSearch();
                }
            }
        }

        public ICommand SelectCommand => new RelayCommand(ExecuteSelect);
        public ICommand CancelCommand => new RelayCommand(ExecuteCancel);

        private void LoadCustomers()
        {
            try
            {
                var list = _customerService.GetAllCustomers();
                Customers = new ObservableCollection<Customer>(list);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void PerformSearch()
        {
            string query = SearchText?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                LoadCustomers();
            }
            else
            {
                var list = _customerService.SearchCustomers(query);
                Customers = new ObservableCollection<Customer>(list);
            }
        }

        private void ExecuteSelect()
        {
            if (SelectedCustomer != null)
            {
                OnCustomerSelected?.Invoke(SelectedCustomer);
                CloseAction?.Invoke();
            }
        }

        private void ExecuteCancel()
        {
            CloseAction?.Invoke();
        }
    }
}
