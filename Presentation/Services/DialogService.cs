using System;
using System.Collections.Generic;
using System.Windows;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string title, string message) => SweetAlert.Info(title, message);
        public void ShowInfo(string title, string message) => SweetAlert.Info(title, message);
        public void ShowError(string title, string message) => SweetAlert.Error(title, message);
        public void ShowWarning(string title, string message) => SweetAlert.Warning(title, message);
        public void ShowSuccess(string title, string message) => SweetAlert.Success(title, message);
        public bool Confirm(string title, string message) => SweetAlert.Confirm(title, message);

        public bool? ShowCashSaleDialog(decimal total, out string customerName, out string customerPhone, out string paymentMethod)
        {
            customerName = null;
            customerPhone = null;
            paymentMethod = "Cash";

            var vm = new CashSaleViewModel(total, this);
            var dialog = new CashSaleDialog();
            dialog.DataContext = vm;
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            vm.CloseAction = (result) =>
            {
                dialog.DialogResult = result;
                dialog.Close();
            };

            if (dialog.ShowDialog() == true)
            {
                customerName = vm.CustomerName;
                customerPhone = vm.CustomerPhone;
                paymentMethod = vm.PaymentMethod;
                return true;
            }
            return false;
        }

        public bool? ShowInputDialog(string title, string message, string defaultValue, out string result)
        {
            result = null;
            var dialog = new InputDialog(title, message, defaultValue);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                result = dialog.Answer;
                return true;
            }
            return false;
        }

        public void ShowInvoiceViewDialog(string invoiceNumber)
        {
             var dialog = new InvoiceViewDialog(invoiceNumber);
             dialog.Owner = System.Windows.Application.Current.MainWindow;
             dialog.ShowDialog();
        }

        public bool? ShowUserFormDialog(UserFormViewModel viewModel)
        {
            var dialog = new UserFormDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            return dialog.ShowDialog();
        }

        public bool? ShowProductFormDialog(ProductFormViewModel viewModel)
        {
            var dialog = new ProductFormDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            return dialog.ShowDialog();
        }

        public void ShowCustomerInvoicesDialog(int customerId, string customerName)
        {
            var dialog = new CustomerInvoicesDialog(customerId, customerName);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public bool? ShowExpenseDialog(ExpenseFormViewModel viewModel)
        {
            var dialog = new ExpenseDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            return dialog.ShowDialog();
        }

        public bool? ShowEmployeeFormDialog(EmployeeFormViewModel viewModel)
        {
            var dialog = new EmployeeFormDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            return dialog.ShowDialog();
        }

        public bool? ShowEmployeeSalaryTransactionDialog(Employee employee, string transactionType, out decimal amount, out string paymentMethod, out DateTime transactionDate, out string notes)
        {
            amount = 0;
            paymentMethod = null;
            transactionDate = DateTime.Now;
            notes = null;

            var dialog = new EmployeeSalaryTransactionDialog(employee, transactionType);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                amount = dialog.Amount;
                paymentMethod = dialog.PaymentMethod;
                transactionDate = dialog.TransactionDate;
                notes = dialog.Notes;
                return true;
            }
            return false;
        }

        public bool? ShowSupplierFormDialog(SupplierFormViewModel viewModel)
        {
            var dialog = new SupplierFormDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            return dialog.ShowDialog();
        }

        public bool? ShowSupplierPurchaseDialog(string supplierName, decimal currentDebt, out AlJohary.ServiceHub.Application.DTOs.SupplierPurchaseDialogResult result)
        {
            result = null;
            var dialog = new SupplierPurchaseDialog(supplierName, currentDebt);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                result = dialog.Result;
                return true;
            }
            return false;
        }

        public bool? ShowSupplierPaymentDialog(string supplierName, decimal currentDebt, out decimal paymentAmount, out string paymentMethod)
        {
            paymentAmount = 0;
            paymentMethod = "كاش";
            var dialog = new SupplierPaymentDialog(supplierName, currentDebt);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                paymentAmount = dialog.PaymentAmount;
                paymentMethod = dialog.PaymentMethod;
                return true;
            }
            return false;
        }

        public void ShowReturnDetailsDialog(int returnId)
        {
            var dialog = new ReturnDetailsDialog(returnId);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public void ShowMainWindow()
        {
            var win = new MainWindow();
            var prev = System.Windows.Application.Current.MainWindow;
            System.Windows.Application.Current.MainWindow = win;
            win.Show();
            prev?.Close();
        }

        public void ShowLoginWindow()
        {
            var win = new LoginWindow();
            var prev = System.Windows.Application.Current.MainWindow;
            System.Windows.Application.Current.MainWindow = win;
            win.Show();
            prev?.Close();
        }
    }
}
