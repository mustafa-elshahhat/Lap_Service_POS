using System;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class SupplierFormViewModel : BaseViewModel
    {
        private string _name;
        private string _phone;
        private string _address;
        private bool _isEdit;
        private string _title;
        private string _saveButtonContent;

        public SupplierFormViewModel(bool isEdit = false)
        {
            IsEdit = isEdit;
            Title = isEdit ? "تعديل بيانات المورد" : "إضافة مورد جديد";
            SaveButtonContent = isEdit ? "حفظ التعديلات" : "حفظ";
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public bool IsEdit
        {
            get => _isEdit;
            set => SetProperty(ref _isEdit, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SaveButtonContent
        {
            get => _saveButtonContent;
            set => SetProperty(ref _saveButtonContent, value);
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(Name))
            {
                errorMessage = "يجب إدخال اسم المورد";
                return false;
            }
            return true;
        }
    }
}
