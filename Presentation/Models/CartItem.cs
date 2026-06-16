using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Models
{
    public class CartItem : BaseViewModel
    {
        private int _index;
        private int _productId;
        private string _productName;
        private string _productCode;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _originalPrice;
        private decimal _purchasePrice;
        private decimal _total;

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        public decimal OriginalPrice
        {
            get => _originalPrice;
            set => SetProperty(ref _originalPrice, value);
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }
    }
}
