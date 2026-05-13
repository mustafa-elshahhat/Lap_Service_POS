using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IAuthService
    {
        Dictionary<string, object> Login(string username, string password);
        void Logout();
        void SetSession(Dictionary<string, object> user);
        Dictionary<string, object> CurrentUser { get; }
        bool IsLoggedIn { get; }
        bool IsAdmin { get; }
        bool IsEmployee { get; }
        double GetMaxDiscount();
        double GetMaxMarkup();
        int GetUserId();
        string GetUserName();

        long CreateUser(string username, string password, string fullName, string role, double maxDiscount = 10.0, double maxMarkup = 20.0);
        void UpdateUser(int userId, string fullName = null, string role = null, double? maxDiscount = null, double? maxMarkup = null);
        void ChangeUserPassword(int userId, string newPassword);
        void DeleteUser(int userId);
        List<Dictionary<string, object>> GetAllUsers(bool includeInactive = false);
        Dictionary<string, object> GetUser(int userId);
    }
}
