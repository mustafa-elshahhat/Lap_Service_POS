using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Application.Interfaces;

namespace AlJohary.ServiceHub.Application.Services
{



    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IDbTransactionManager _txManager;
        private Dictionary<string, object> _currentUser;

        public AuthService(IUserRepository userRepo, IDbTransactionManager txManager = null)
        {
            _userRepo = userRepo;
            _txManager = txManager;
        }

        private static IAuthService _instance;

        public static IAuthService Instance
        {
            get => _instance;
            set => _instance = value;
        }

        private Dictionary<string, object> UserToDictionary(User user)
        {
            if (user == null) return null;
            return new Dictionary<string, object>
            {
                { "id", user.Id },
                { "username", user.Username },
                { "full_name", user.FullName },
                { "role", user.Role },
                { "employee_id", user.EmployeeId },
                { "employee_name", string.IsNullOrWhiteSpace(user.EmployeeName) ? "-" : user.EmployeeName },
                { "max_discount_percent", user.MaxDiscountPercent },
                { "max_markup_percent", user.MaxMarkupPercent },
                { "is_active", user.IsActive }
            };
        }

        public Dictionary<string, object> Login(string username, string password)
        {
            var user = _userRepo.Authenticate(username, password);
            if (user != null)
            {
                _currentUser = UserToDictionary(user);
            }
            return _currentUser;
        }

        public void Logout() => _currentUser = null;
        public void SetSession(Dictionary<string, object> user) => _currentUser = user;
        public Dictionary<string, object> CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;
        public bool IsAdmin => _currentUser != null && SafeConvert.ToString(_currentUser["role"]) == "admin";
        public bool IsEmployee => _currentUser != null && SafeConvert.ToString(_currentUser["role"]) == "employee";
        public bool CanBypassPriceLimits => IsAdmin;

        public double GetMaxDiscount() => _currentUser != null ? SafeConvert.ToDouble(_currentUser["max_discount_percent"]) : 0;
        public double GetMaxMarkup() => _currentUser != null ? SafeConvert.ToDouble(_currentUser["max_markup_percent"]) : 0;
        public int GetUserId() => _currentUser != null ? SafeConvert.ToInt(_currentUser["id"]) : 0;
        public string GetUserName() => _currentUser != null ? SafeConvert.ToString(_currentUser["full_name"]) : "";

        #region User Management (Admin Only)

        public long CreateUser(string username, string password, string fullName, string role,
            double maxDiscount = 10.0, double maxMarkup = 20.0, int? employeeId = null)
        {
            if (!IsAdmin) throw new UnauthorizedAccessException("ليس لديك صلاحية إنشاء مستخدمين");
            if (_userRepo.UsernameExists(username)) throw new InvalidOperationException("اسم المستخدم موجود بالفعل");
            ValidateEmployeeLink(employeeId, null);

            string passwordHash = Security.HashPassword(password);
            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                FullName = fullName,
                Role = role,
                EmployeeId = employeeId,
                MaxDiscountPercent = maxDiscount,
                MaxMarkupPercent = maxMarkup
            };

            return _userRepo.Create(user);
        }

        public void UpdateUser(int userId, string fullName = null, string role = null,
            double? maxDiscount = null, double? maxMarkup = null, int? employeeId = null, bool updateEmployeeLink = false)
        {
            if (!IsAdmin) throw new UnauthorizedAccessException("ليس لديك صلاحية تعديل المستخدمين");
            if (updateEmployeeLink) ValidateEmployeeLink(employeeId, userId);

            var user = new User { Id = userId, MaxDiscountPercent = -1.0, MaxMarkupPercent = -1.0 };
            if (fullName != null) user.FullName = fullName;
            if (role != null) user.Role = role;
            if (maxDiscount.HasValue) user.MaxDiscountPercent = maxDiscount.Value;
            if (maxMarkup.HasValue) user.MaxMarkupPercent = maxMarkup.Value;

            if (!updateEmployeeLink)
            {
                _userRepo.Update(user);
                return;
            }

            if (_txManager == null)
            {
                // No transaction manager wired (e.g. in unit tests): fall back to sequential writes.
                _userRepo.Update(user);
                _userRepo.UpdateEmployeeLink(userId, employeeId);
                return;
            }

            // Update the profile and the employee link atomically so a constraint failure on the link
            // (e.g. the unique active-employee index) does not leave a partially-applied edit.
            _txManager.BeginTransaction();
            try
            {
                _userRepo.Update(user);
                _userRepo.UpdateEmployeeLink(userId, employeeId);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void ChangeUserPassword(int userId, string newPassword)
        {
            if (!IsAdmin && GetUserId() != userId) throw new UnauthorizedAccessException("ليس لديك صلاحية تغيير كلمة المرور");
            _userRepo.ChangePassword(userId, Security.HashPassword(newPassword));
        }

        public void DeleteUser(int userId)
        {
            if (!IsAdmin) throw new UnauthorizedAccessException("ليس لديك صلاحية حذف المستخدمين");
            if (GetUserId() == userId) throw new InvalidOperationException("لا يمكنك حذف حسابك الخاص");

            var user = _userRepo.GetById(userId);
            if (user != null && user.Role == "admin" && _userRepo.GetAdminsCount() <= 1)
                throw new InvalidOperationException("لا يمكن حذف آخر مدير في النظام");

            _userRepo.Delete(userId);
        }

        public List<Dictionary<string, object>> GetAllUsers(bool includeInactive = false)
        {
            if (!IsAdmin) throw new UnauthorizedAccessException("ليس لديك صلاحية عرض المستخدمين");
            var users = _userRepo.GetAll(includeInactive);
            var list = new List<Dictionary<string, object>>();
            foreach (var u in users) list.Add(UserToDictionary(u));
            return list;
        }

        public Dictionary<string, object> GetUser(int userId)
        {
            if (!IsAdmin && GetUserId() != userId) throw new UnauthorizedAccessException("ليس لديك صلاحية عرض بيانات هذا المستخدم");
            return UserToDictionary(_userRepo.GetById(userId));
        }

        private void ValidateEmployeeLink(int? employeeId, int? exceptUserId)
        {
            if (!employeeId.HasValue) return;

            if (!_userRepo.ActiveEmployeeExists(employeeId.Value))
                throw new InvalidOperationException("الموظف المحدد غير نشط أو غير موجود");

            if (_userRepo.IsEmployeeLinkedToActiveUser(employeeId.Value, exceptUserId))
                throw new InvalidOperationException("هذا الموظف مرتبط بالفعل بحساب مستخدم نشط");
        }

        #endregion
    }
}
