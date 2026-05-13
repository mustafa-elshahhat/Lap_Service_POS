using System;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CustomerFormViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly IDialogService _dialogService;
        public Action<bool> CloseAction { get; set; }

        private int? _id;
        private string _name;
        private string _phone;
        private string _address;
        private string _notes;
        private string _title = "إضافة عميل جديد";

        public CustomerFormViewModel(IDialogService dialogService, Customer existingCustomer = null)
        {
            _customerService = ServiceContainer.GetService<ICustomerService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();

            if (existingCustomer != null)
            {
                _id = existingCustomer.Id;
                Name = existingCustomer.Name;
                Phone = existingCustomer.Phone;
                Address = existingCustomer.Address;
                Notes = existingCustomer.Notes;
                Title = "تعديل بيانات العميل";
            }
        }

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
        public string Address { get => _address; set => SetProperty(ref _address, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand CancelCommand => new RelayCommand(Cancel);

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                _dialogService.ShowWarning("تنبيه", "اسم العميل مطلوب");
                return;
            }

            try
            {
                var customer = new Customer
                {
                    Id = _id ?? 0,
                    Name = Name,
                    Phone = Phone,
                    Address = Address,
                    Notes = Notes
                };

                if (customer.Id > 0) _customerService.UpdateCustomer(customer);
                else _customerService.CreateCustomer(customer);
                CloseAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}
