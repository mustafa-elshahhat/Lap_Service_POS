using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class ReturnsViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IReturnService _returnService;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;
        private ObservableCollection<Return> _returns;
        private Return _selectedReturn;
        private string _searchText;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private Visibility _emptyStateVisibility;

        public ReturnsViewModel(IDialogService dialogService = null)
        {
            _saleService = ServiceContainer.GetService<ISaleService>();
            _returnService = ServiceContainer.GetService<IReturnService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _printService = ServiceContainer.GetService<IPrintService>();
            _startDate = new DateTime(2020, 1, 1);
            _endDate = DateTime.Today.AddDays(1);
            Returns = new ObservableCollection<Return>();
            LoadReturns();
        }

        public ObservableCollection<Return> Returns
        {
            get => _returns;
            set
            {
                _returns = value;
                OnPropertyChanged();
            }
        }

        public Return SelectedReturn
        {
            get => _selectedReturn;
            set
            {
                _selectedReturn = value;
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
                    LoadReturns();
                }
            }
        }

        public Visibility EmptyStateVisibility
        {
            get => _emptyStateVisibility;
            set { _emptyStateVisibility = value; OnPropertyChanged(); }
        }

        public ICommand SearchCommand => new RelayCommand(LoadReturns);
        public ICommand LoadAllCommand => new RelayCommand(LoadAll);
        public ICommand NewReturnCommand => new RelayCommand(NewReturn);
        public ICommand ViewDetailsCommand => new RelayCommand(ViewDetails, () => SelectedReturn != null);
        public ICommand ViewInvoiceCommand => new RelayCommand(ViewInvoice, () => SelectedReturn != null);
        public ICommand PrintReturnCommand => new RelayCommand(PrintReturn, () => SelectedReturn != null);

        private void LoadAll()
        {
            SearchText = "";
            LoadReturns();
            OnRequestSearchFocus();
        }

        private void LoadReturns()
        {
            try
            {
                string query = SearchText?.Trim();
                string sDate = _startDate?.ToString("yyyy-MM-dd");
                string eDate = _endDate?.ToString("yyyy-MM-dd");

                var results = _returnService.GetReturnsReport(sDate, eDate);

                if (!string.IsNullOrEmpty(query))
                {
                    string q = query.ToLower();
                    results = results.FindAll(r =>
                        (r.ReturnNumber?.ToLower().Contains(q) ?? false) ||
                        (r.InvoiceNumber?.ToLower().Contains(q) ?? false));
                }

                Returns = new ObservableCollection<Return>(results);
                EmptyStateVisibility = Returns.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void NewReturn()
        {
             try
            {
                if (_dialogService.ShowInputDialog("مرتجع جديد", "أدخل رقم الفاتورة:", "", out string invoice) == true)
                {
                    invoice = invoice?.Trim();
                    if (string.IsNullOrEmpty(invoice))
                    {
                        _dialogService.ShowWarning("تنبيه", "الرجاء إدخال رقم فاتورة صالح");
                        return;
                    }

                    _dialogService.ShowInvoiceViewDialog(invoice);
                    LoadReturns();
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
            OnRequestSearchFocus();
        }

        private void ViewDetails()
        {
            if (SelectedReturn != null)
            {
                 _dialogService.ShowReturnDetailsDialog(SelectedReturn.Id);
            }
            OnRequestSearchFocus();
        }

        private void ViewInvoice()
        {
             if (SelectedReturn != null)
            {
                 string invoiceNum = SelectedReturn.InvoiceNumber;
                 _dialogService.ShowInvoiceViewDialog(invoiceNum);
            }
            OnRequestSearchFocus();
        }

        private void PrintReturn()
        {
            if (SelectedReturn != null)
            {
                var returnData = _returnService.GetReturnById((int)SelectedReturn.Id);
                if (returnData != null)
                {
                    var itemsData = _returnService.GetReturnItems((int)SelectedReturn.Id);

                    var items = new List<ReturnItem>();
                    foreach (var dict in itemsData)
                    {
                        items.Add(new ReturnItem {
                            ProductName = SafeConvert.ToString(dict["product_name"]),
                            Quantity = SafeConvert.ToInt(dict["quantity"]),
                            TotalPrice = SafeConvert.ToDecimal(dict["total_price"])
                        });
                    }

                    _printService.PrintReturnReceipt(SelectedReturn, items);
                }
            }
            OnRequestSearchFocus();
        }
    }
}
