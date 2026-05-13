using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class InvoicesViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Sale> _invoices;
        private Sale _selectedInvoice;
        private string _searchText;

        public InvoicesViewModel(IDialogService dialogService = null)
        {
            _saleService = ServiceContainer.GetService<ISaleService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            LoadAll();
        }

        public ObservableCollection<Sale> Invoices
        {
            get => _invoices;
            set
            {
                _invoices = value;
                OnPropertyChanged();
            }
        }

        public Sale SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    Search();
                }
            }
        }

        public ICommand SearchCommand => new RelayCommand(Search);
        public ICommand LoadAllCommand => new RelayCommand(LoadAll);
        public ICommand ViewInvoiceCommand => new RelayCommand(ViewInvoice, () => SelectedInvoice != null);
        public ICommand PrintInvoiceCommand => new RelayCommand(PrintInvoice, () => SelectedInvoice != null);

        private void LoadToday()
        {
            try
            {
                var list = _saleService.GetSales();
                Invoices = new ObservableCollection<Sale>(list ?? new List<Sale>());
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void Search()
        {
            try
            {
                var q = SearchText?.Trim() ?? "";
                var list = _saleService.GetSales(q);
                Invoices = new ObservableCollection<Sale>(list ?? new List<Sale>());
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void LoadAll()
        {
            SearchText = "";
            Search();
            OnRequestSearchFocus();
        }

        private void ViewInvoice()
        {
            if (SelectedInvoice == null) return;
            ShowInvoiceDialog(SelectedInvoice, false);
        }

        private void PrintInvoice()
        {
            if (SelectedInvoice == null) return;
            ShowInvoiceDialog(SelectedInvoice, true);
        }

        private void ShowInvoiceDialog(Sale invoiceRow, bool autoPrint)
        {
            try
            {
                string invoiceNum = invoiceRow.InvoiceNumber;

                if (autoPrint)
                {
                    var dialog = new InvoiceViewDialog(invoiceNum);
                    var owner = System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                    if (owner != null) dialog.Owner = owner;
                    dialog.PrintInvoice();
                }
                else
                {
                    _dialogService.ShowInvoiceViewDialog(invoiceNum);
                }
                OnRequestSearchFocus();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
                OnRequestSearchFocus();
            }
        }
    }
}
