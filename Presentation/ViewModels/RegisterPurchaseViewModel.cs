using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class RegisterPurchaseViewModel : BaseViewModel
    {
        private ObservableCollection<AdHocItemViewModel> _items;
        public ObservableCollection<string> AvailablePaymentMethods { get; private set; }

        private string _selectedPaymentMethod;
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public Action<bool> CloseAction { get; set; }

        public RegisterPurchaseViewModel()
        {
            Items = new ObservableCollection<AdHocItemViewModel>();

            AvailablePaymentMethods = new ObservableCollection<string>(PaymentMethods.GetAll());
            AvailablePaymentMethods.Add("آجل");

            SelectedPaymentMethod = PaymentMethods.Cash;

            AddItem();
        }

        public ObservableCollection<AdHocItemViewModel> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public ICommand AddItemCommand => new RelayCommand(AddItem);
        public ICommand RemoveItemCommand => new RelayCommand<AdHocItemViewModel>(RemoveItem);
        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand CancelCommand => new RelayCommand(Cancel);

        private void AddItem()
        {
            Items.Add(new AdHocItemViewModel());
        }

        private void RemoveItem(AdHocItemViewModel item)
        {
            if (Items.Count > 1)
                Items.Remove(item);
        }

        private void Save()
        {

            if (Items.Any(i => string.IsNullOrWhiteSpace(i.ProductName)))
            {

                return;
            }
            CloseAction?.Invoke(true);
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }

    public class AdHocItemViewModel : BaseViewModel
    {
        private string _productName;
        private string _price = "0";
        private string _profit = "0";

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public string Profit
        {
            get => _profit;
            set => SetProperty(ref _profit, value);
        }

        public decimal DecimalPrice => SafeConvert.ToDecimal(Price);
        public decimal DecimalProfit => SafeConvert.ToDecimal(Profit);
    }
}
