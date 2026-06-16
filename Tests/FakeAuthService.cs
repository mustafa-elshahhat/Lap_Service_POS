using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;

namespace AlJohary.ServiceHub.Tests
{
    /// <summary>
    /// Minimal in-test IAuthService. Only the members exercised by the financial-flow services are
    /// meaningful; the rest return harmless defaults.
    /// </summary>
    public class FakeAuthService : IAuthService
    {
        public int UserId { get; set; } = 1;
        public bool Admin { get; set; } = true;
        public bool BypassPriceLimits { get; set; } = true;
        public double MaxDiscount { get; set; } = 0;
        public double MaxMarkup { get; set; } = 0;

        public Dictionary<string, object> Login(string username, string password) => null;
        public void Logout() { }
        public void SetSession(Dictionary<string, object> user) { }
        public Dictionary<string, object> CurrentUser => null;
        public bool IsLoggedIn => true;
        public bool IsAdmin => Admin;
        public bool IsEmployee => !Admin;
        public bool CanBypassPriceLimits => BypassPriceLimits;
        public double GetMaxDiscount() => MaxDiscount;
        public double GetMaxMarkup() => MaxMarkup;
        public int GetUserId() => UserId;
        public string GetUserName() => "test";

        public long CreateUser(string username, string password, string fullName, string role, double maxDiscount = 10.0, double maxMarkup = 20.0, int? employeeId = null) => 0;
        public void UpdateUser(int userId, string fullName = null, string role = null, double? maxDiscount = null, double? maxMarkup = null, int? employeeId = null, bool updateEmployeeLink = false) { }
        public void ChangeUserPassword(int userId, string newPassword) { }
        public void DeleteUser(int userId) { }
        public List<Dictionary<string, object>> GetAllUsers(bool includeInactive = false) => new List<Dictionary<string, object>>();
        public Dictionary<string, object> GetUser(int userId) => null;
        public bool IsForcePasswordChangeRequired() => false;
        public void ClearForcePasswordChangeFlag() { }
    }
}
