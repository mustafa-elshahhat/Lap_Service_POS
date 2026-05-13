using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CustomersViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly ICustomerService _customerService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private string _searchText;

        public CustomersViewModel(IDialogService dialogService = null)
        {
            _saleService = ServiceContainer.GetService<ISaleService>();
            _customerService = ServiceContainer.GetService<ICustomerService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            Customers = new ObservableCollection<Customer>();
            LoadCustomers();
        }

        #region Properties

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

        #endregion

        #region Commands

        public ICommand SearchCommand => new RelayCommand(PerformSearch);
        public ICommand ShowAllCommand => new RelayCommand(() => { LoadCustomers(); OnRequestSearchFocus(); });
        public ICommand ViewDetailsCommand => new RelayCommand(ViewDetails, () => SelectedCustomer != null);
        public ICommand AddCustomerCommand => new RelayCommand(AddCustomer);
        public ICommand RefreshCommand => new RelayCommand(LoadCustomers);

        #endregion

        #region Methods

        private void LoadCustomers()
        {
            try
            {
                var list = _customerService.GetAllCustomers();
                UpdateCustomersList(list);
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
                UpdateCustomersList(list);
            }
        }

        private void UpdateCustomersList(List<Customer> list)
        {
            Customers.Clear();
            foreach (var item in list)
            {
                Customers.Add(item);
            }
        }

        private void ViewDetails()
        {
            if (SelectedCustomer == null) return;

            try
            {
                _dialogService.ShowCustomerInvoicesDialog(
                    SelectedCustomer.Id,
                    SelectedCustomer.Name
                );
                OnRequestSearchFocus();
            }
            catch (Exception ex)
            {
                 _dialogService.ShowError("خطأ", ex.Message);
                 OnRequestSearchFocus();
            }
        }

        private void AddCustomer()
        {
            try
            {
                var vm = new CustomerFormViewModel(_dialogService);
                var view = new CustomerFormDialog
                {
                    DataContext = vm,
                    Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                };

                vm.CloseAction = (result) =>
                {
                    if (result)
                    {
                        view.Close();
                        LoadCustomers();
                        _dialogService.ShowSuccess("تم", "تم إضافة العميل بنجاح");
                    }
                    else
                    {
                        view.Close();
                    }
                };

                view.ShowDialog();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        #endregion
    }
}
