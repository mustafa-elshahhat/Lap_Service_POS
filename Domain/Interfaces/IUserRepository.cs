using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Domain.Interfaces
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
        User Authenticate(string username, string password);
    }
}
