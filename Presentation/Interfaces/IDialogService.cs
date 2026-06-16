using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Interfaces
{
    public interface IDialogService
    {
        void ShowMessage(string title, string message);
        void ShowInfo(string title, string message);
        void ShowError(string title, string message);
        void ShowWarning(string title, string message);
        void ShowSuccess(string title, string message);
        bool Confirm(string title, string message);

        bool? ShowCashSaleDialog(decimal total, out string customerName, out string customerPhone, out string paymentMethod);

        bool? ShowInputDialog(string title, string message, string defaultValue, out string result);
        void ShowInvoiceViewDialog(string invoiceNumber);
        bool? ShowUserFormDialog(UserFormViewModel viewModel);
        bool? ShowProductFormDialog(ProductFormViewModel viewModel);
        void ShowCustomerInvoicesDialog(int customerId, string customerName);
        bool? ShowExpenseDialog(ExpenseFormViewModel viewModel);
        bool? ShowEmployeeFormDialog(EmployeeFormViewModel viewModel);
        bool? ShowEmployeeSalaryTransactionDialog(Employee employee, string transactionType, out decimal amount, out string paymentMethod, out DateTime transactionDate, out string notes);
        bool? ShowSupplierFormDialog(SupplierFormViewModel viewModel);
        bool? ShowSupplierPurchaseDialog(string supplierName, decimal currentDebt, out decimal purchaseAmount, out string paymentMethod, out decimal paidAmount);
        bool? ShowSupplierPaymentDialog(string supplierName, decimal currentDebt, out decimal paymentAmount, out string paymentMethod);
        void ShowSupplierTransactionsDialog(int supplierId, string supplierName);
        void ShowReturnDetailsDialog(int returnId);

        void ShowMainWindow();
        void ShowLoginWindow();
    }
}
