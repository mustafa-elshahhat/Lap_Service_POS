using System;

namespace AlJohary.ServiceHub.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public double MaxDiscountPercent { get; set; }
        public double MaxMarkupPercent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
