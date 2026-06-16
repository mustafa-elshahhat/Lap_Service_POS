using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class EmployeeFormViewModel : BaseViewModel
    {
        private string _fullName;
        private string _phone;
        private string _jobTitle;
        private decimal _baseSalary;
        private string _notes;
        private string _title;
        private string _saveButtonContent;

        public EmployeeFormViewModel(bool isEdit = false)
        {
            Title = isEdit ? "تعديل بيانات الموظف" : "إضافة موظف جديد";
            SaveButtonContent = isEdit ? "حفظ التعديلات" : "حفظ";
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string JobTitle
        {
            get => _jobTitle;
            set => SetProperty(ref _jobTitle, value);
        }

        public decimal BaseSalary
        {
            get => _baseSalary;
            set => SetProperty(ref _baseSalary, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
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
            return EmployeeValidator.TryValidate(FullName, BaseSalary, out errorMessage);
        }
    }
}
