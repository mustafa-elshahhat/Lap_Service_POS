using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IUserRepository
    {
        User GetById(int id);
        User GetByUsername(string username);
        List<User> GetAll(bool includeInactive = false);
        long Create(User user);
        void Update(User user);
        void ChangePassword(int userId, string newPasswordHash);
        void Delete(int id);
        int GetAdminsCount();
        bool UsernameExists(string username);
        bool ActiveEmployeeExists(int employeeId);
        bool IsEmployeeLinkedToActiveUser(int employeeId, int? exceptUserId = null);
        void UpdateEmployeeLink(int userId, int? employeeId);
        User Authenticate(string username, string password);
    }
}
