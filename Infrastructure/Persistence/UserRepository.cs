using System;
using System.Collections.Generic;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseManager _db;

        public UserRepository()
        {
            _db = DatabaseManager.Instance;
        }

        private User MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;
            return new User
            {
                Id = SafeConvert.ToInt(row["id"]),
                Username = SafeConvert.ToString(row["username"]),
                PasswordHash = SafeConvert.ToString(row["password_hash"]),
                FullName = SafeConvert.ToString(row["full_name"]),
                Role = SafeConvert.ToString(row["role"]),
                MaxDiscountPercent = SafeConvert.ToDouble(row["max_discount_percent"]),
                MaxMarkupPercent = SafeConvert.ToDouble(row["max_markup_percent"]),
                IsActive = SafeConvert.ToBool(row["is_active"]),
                CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue,
                UpdatedAt = SafeConvert.ToDateTime(row["updated_at"]) ?? DateTime.MinValue
            };
        }

        public User GetById(int id)
        {
            var row = _db.FetchOne("SELECT * FROM users WHERE id = @id", new Dictionary<string, object> { { "@id", id } });
            return MapToEntity(row);
        }

        public User GetByUsername(string username)
        {
            var row = _db.FetchOne("SELECT * FROM users WHERE username = @username", new Dictionary<string, object> { { "@username", username } });
            return MapToEntity(row);
        }

        public List<User> GetAll(bool includeInactive = false)
        {
            string sql = "SELECT * FROM users";
            if (!includeInactive) sql += " WHERE is_active = 1";
            sql += " ORDER BY full_name";
            
            var rows = _db.FetchAll(sql);
            var list = new List<User>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public long Create(User user)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO users (username, password_hash, full_name, role, max_discount_percent, max_markup_percent, is_active, created_at, updated_at)
                VALUES (@username, @password, @fullName, @role, @maxDiscount, @maxMarkup, 1, datetime('now'), datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@username", user.Username },
                    { "@password", user.PasswordHash },
                    { "@fullName", user.FullName },
                    { "@role", user.Role },
                    { "@maxDiscount", user.MaxDiscountPercent },
                    { "@maxMarkup", user.MaxMarkupPercent }
                });
        }

        public void Update(User user)
        {
             string sql = @"UPDATE users SET updated_at = datetime('now')";
             var args = new Dictionary<string, object> { { "@id", user.Id } };

             if (user.FullName != null) { sql += ", full_name = @fullName"; args.Add("@fullName", user.FullName); }
             if (user.Role != null) { sql += ", role = @role"; args.Add("@role", user.Role); }
             if (user.MaxDiscountPercent >= 0) { sql += ", max_discount_percent = @maxDiscount"; args.Add("@maxDiscount", user.MaxDiscountPercent); }
             if (user.MaxMarkupPercent >= 0) { sql += ", max_markup_percent = @maxMarkup"; args.Add("@maxMarkup", user.MaxMarkupPercent); }
             
             sql += " WHERE id = @id";
             _db.Execute(sql, args);
        }

        public void ChangePassword(int userId, string newPasswordHash)
        {
            _db.Execute("UPDATE users SET password_hash = @password, updated_at = datetime('now') WHERE id = @id",
                new Dictionary<string, object> { { "@password", newPasswordHash }, { "@id", userId } });
        }

        public void Delete(int id)
        {
            _db.Execute("UPDATE users SET is_active = 0, updated_at = datetime('now') WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
        }

        public int GetAdminsCount()
        {
            var row = _db.FetchOne("SELECT COUNT(*) as count FROM users WHERE role = 'admin' AND is_active = 1");
            return SafeConvert.ToInt(row["count"]);
        }

        public bool UsernameExists(string username)
        {
             var row = _db.FetchOne("SELECT id FROM users WHERE username = @username", new Dictionary<string, object> { { "@username", username } });
             return row != null;
        }

        public User Authenticate(string username, string password)
        {
             var user = GetByUsername(username);
             if (user != null && user.IsActive && Security.VerifyPassword(password, user.PasswordHash))
             {
                 return user;
             }
             return null;
        }
    }
}
