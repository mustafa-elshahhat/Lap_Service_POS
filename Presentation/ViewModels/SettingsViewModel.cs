using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Shared.Helpers;
using System;
using System.IO;
using System.Windows.Input;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        private string _shopName;
        private string _shopAddress;
        private string _shopPhone;
        private int _maxDiscountPercent;
        private int _maxMarkupPercent;
        private int _lowStockThreshold;
        public SettingsViewModel(IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _settingsService = ServiceContainer.GetService<ISettingsService>();
            LoadSettings();
        }

        #region Properties

        public string ShopName
        {
            get => _shopName;
            set => SetProperty(ref _shopName, value);
        }

        public string ShopAddress
        {
            get => _shopAddress;
            set => SetProperty(ref _shopAddress, value);
        }

        public string ShopPhone
        {
            get => _shopPhone;
            set => SetProperty(ref _shopPhone, value);
        }

        public int MaxDiscountPercent
        {
            get => _maxDiscountPercent;
            set => SetProperty(ref _maxDiscountPercent, value);
        }

        public int MaxMarkupPercent
        {
            get => _maxMarkupPercent;
            set => SetProperty(ref _maxMarkupPercent, value);
        }

        public int LowStockThreshold
        {
            get => _lowStockThreshold;
            set => SetProperty(ref _lowStockThreshold, value);
        }

        #endregion

        #region Commands

        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);
        public ICommand BackupDatabaseCommand => new RelayCommand(BackupDatabase);
        public ICommand RestoreDatabaseCommand => new RelayCommand(RestoreDatabase);

        #endregion

        #region Methods

        private void LoadSettings()
        {
            ShopName = _settingsService.GetSetting("shop_name", "الجوهري");
            ShopAddress = _settingsService.GetSetting("shop_address", "");
            ShopPhone = _settingsService.GetSetting("shop_phone", "");

            MaxDiscountPercent = (int)SafeConvert.ToDecimal(_settingsService.GetSetting("max_discount_percent", "10.0"));
            MaxMarkupPercent = (int)SafeConvert.ToDecimal(_settingsService.GetSetting("max_markup_percent", "20.0"));
            LowStockThreshold = SafeConvert.ToInt(_settingsService.GetSetting("low_stock_threshold", "5"));
        }

        private void SaveSettings()
        {
            if (string.IsNullOrWhiteSpace(ShopName))
            {
                _dialogService.ShowWarning("تنبيه", "اسم المحل مطلوب");
                return;
            }

            try
            {
                _settingsService.SetSetting("shop_name", ShopName);
                _settingsService.SetSetting("shop_address", ShopAddress);
                _settingsService.SetSetting("shop_phone", ShopPhone);
                _settingsService.SetSetting("max_discount_percent", MaxDiscountPercent.ToString());
                _settingsService.SetSetting("max_markup_percent", MaxMarkupPercent.ToString());
                _settingsService.SetSetting("low_stock_threshold", LowStockThreshold.ToString());
                _dialogService.ShowSuccess("نجاح", "تم حفظ الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void BackupDatabase()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                    Filter = "Database Files|*.db|All Files|*.*",
                    Title = "حفظ نسخة احتياطية"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.Copy(_settingsService.GetDatabasePath(), dialog.FileName, true);
                    _dialogService.ShowSuccess("نجاح", "تم إنشاء نسخة احتياطية بنجاح");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل إنشاء النسخة الاحتياطية: " + ex.Message);
            }
        }

        private void RestoreDatabase()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Database Files|*.db|All Files|*.*",
                    Title = "اختيار نسخة للاستعادة"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_dialogService.Confirm("تأكيد الاستعادة", "تحذير: استعادة النسخة سيؤدي إلى حذف البيانات الحالية واستبدالها. هل تريد الاستمرار؟"))
                    {
                        _settingsService.RestoreDatabase(dialog.FileName);
                        _dialogService.ShowSuccess("نجاح", "تم استعادة النسخة الاحتياطية بنجاح. سيتم تطبيق التغييرات فوراً.");
                        LoadSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل استعادة النسخة: " + ex.Message);
            }
        }

        #endregion
    }
}
