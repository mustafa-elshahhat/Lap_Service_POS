using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CustomerInvoicesViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IDialogService _dialogService;
        private readonly int _customerId;
        private readonly string _customerName;

        private List<Sale> _allInvoices;
        private List<Sale> _filteredInvoices;
        private string _searchText;
        private string _filterType;
        private Sale _selectedInvoice;
        private string _salesCountText;
        private bool _isEmpty;

        public Action CloseAction { get; set; }

        public CustomerInvoicesViewModel(int customerId, string customerName, IDialogService dialogService = null, ISaleService saleService = null)
        {
            _saleService = saleService ?? ServiceContainer.GetService<ISaleService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _customerId = customerId;
            _customerName = customerName;
            _filterType = "All";

            LoadInvoices();
        }

        #region Properties

        public string CustomerNameHeader => $"العميل: {_customerName}";

        public string SalesCountText
        {
            get => _salesCountText;
            set => SetProperty(ref _salesCountText, value);
        }

        public List<Sale> Invoices
        {
            get => _filteredInvoices;
            set => SetProperty(ref _filteredInvoices, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Sale SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        #endregion

        #region Commands

        public ICommand CloseCommand => new RelayCommand(Close);
        public ICommand ViewDetailsCommand => new RelayCommand(ViewDetails, () => SelectedInvoice != null);
        public ICommand PrintCommand => new RelayCommand(Print, () => SelectedInvoice != null);

        #endregion

        #region Methods

        public void LoadInvoices()
        {
            try
            {
                _allInvoices = _saleService.GetSalesByCustomer(_customerId);
                ApplyFilters();
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void ApplyFilters()
        {
            if (_allInvoices == null) return;
            
            var query = SearchText?.ToLower();
            string typeFilter = FilterType; 

            var filtered = _allInvoices.Where(i => 
            {
                bool matchText = string.IsNullOrEmpty(query) || 
                                 (i.InvoiceNumber?.ToLower().Contains(query) ?? false);
                
                bool matchType =  typeFilter == "All" || string.IsNullOrEmpty(typeFilter) || 
                                  i.SaleType == typeFilter;
                                  
                return matchText && matchType;
            }).ToList();

            Invoices = filtered;
            SalesCountText = $" | عدد الفواتير: {filtered.Count}";
            IsEmpty = filtered.Count == 0;
        }

        private void ViewDetails()
        {
            if (SelectedInvoice == null) return;
            string invNum = SelectedInvoice.InvoiceNumber;
            _dialogService.ShowInvoiceViewDialog(invNum);
        }

        public ICommand PrintStatementCommand => new RelayCommand(PrintStatement);

        private void Print()
        {
            if (SelectedInvoice == null) return;
            _dialogService.ShowInvoiceViewDialog(SelectedInvoice.InvoiceNumber);
        }

        private void PrintStatement()
        {
            if (Invoices == null || Invoices.Count == 0)
            {
                _dialogService.ShowInfo("تنبيه", "لا توجد فواتير للطباعة");
                return;
            }

            try
            {
                var printService = ServiceContainer.GetService<IPrintService>();
                var groupedData = new List<CarPartsShopWPF.Application.DTOs.GroupedReportItem>();

                foreach (var inv in Invoices)
                {
                    var items = _saleService.GetSaleItems((int)inv.Id);
                    
                    var headerParams = new Dictionary<string, object>
                    {
                        { "رقم الفاتورة", inv.InvoiceNumber },
                        { "التاريخ", inv.SaleDate.ToString("yyyy-MM-dd") },
                        { "النوع", "كاش" },
                        { "الإجمالي", Formatting.FormatCurrency(inv.TotalAmount) },
                        { "المدفوع", Formatting.FormatCurrency(inv.PaidAmount) },
                        { "المتبقي", Formatting.FormatCurrency(inv.RemainingAmount) }
                    };

                    var itemsList = new List<Dictionary<string, object>>();
                    foreach(var item in items)
                    {
                        itemsList.Add(new Dictionary<string, object>
                        {
                            { "Product", item.ProductName },
                            { "Qty", item.Quantity },
                            { "Price", Formatting.FormatCurrency(item.UnitFinalPrice) },
                            { "Total", Formatting.FormatCurrency(item.TotalPrice) }
                        });
                    }

                    groupedData.Add(new CarPartsShopWPF.Application.DTOs.GroupedReportItem
                    {
                        GroupHeader = headerParams,
                        Items = itemsList
                    });
                }

                string title = $"كشف حساب مفصل - {_customerName}";
                string[] cols = { "Total", "Price", "Qty", "Product" };
                string[] headers = { "الإجمالي", "السعر", "الكمية", "المنتج" };

                printService.PrintGroupedReport(title, groupedData, cols, headers);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل الطباعة: " + ex.Message);
            }
        }

        private void Close()
        {
            CloseAction?.Invoke();
        }

        #endregion
    }
}
