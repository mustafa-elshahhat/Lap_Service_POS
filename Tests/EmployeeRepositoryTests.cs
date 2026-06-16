using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class EmployeeRepositoryTests
    {
        public EmployeeRepositoryTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        [Fact]
        public void Create_PersistsEmployee()
        {
            var repo = new EmployeeRepository();

            long id = repo.Create(new Employee
            {
                FullName = "موظف جديد",
                Phone = "01000000000",
                JobTitle = "فني",
                BaseSalary = 3500m,
                Notes = "ملاحظة"
            });

            var employee = repo.GetById((int)id);

            Assert.NotNull(employee);
            Assert.Equal("موظف جديد", employee.FullName);
            Assert.Equal("01000000000", employee.Phone);
            Assert.Equal("فني", employee.JobTitle);
            Assert.Equal(3500m, employee.BaseSalary);
            Assert.True(employee.IsActive);
        }

        [Fact]
        public void Update_ChangesEmployeeDetails()
        {
            var repo = new EmployeeRepository();
            long id = repo.Create(new Employee { FullName = "قبل", BaseSalary = 1000m });

            repo.Update(new Employee
            {
                Id = (int)id,
                FullName = "بعد",
                Phone = "01111111111",
                JobTitle = "مسؤول",
                BaseSalary = 2000m,
                Notes = "تم التعديل"
            });

            var employee = repo.GetById((int)id);

            Assert.Equal("بعد", employee.FullName);
            Assert.Equal("01111111111", employee.Phone);
            Assert.Equal("مسؤول", employee.JobTitle);
            Assert.Equal(2000m, employee.BaseSalary);
            Assert.Equal("تم التعديل", employee.Notes);
        }

        [Fact]
        public void SetActive_TogglesEmployeeStatus()
        {
            var repo = new EmployeeRepository();
            long id = repo.Create(new Employee { FullName = "موظف", BaseSalary = 1000m });

            repo.SetActive((int)id, false);

            Assert.False(repo.GetById((int)id).IsActive);

            repo.SetActive((int)id, true);

            Assert.True(repo.GetById((int)id).IsActive);
        }
    }
}
