namespace AlJohary.ServiceHub.Shared.Helpers
{
    /// <summary>
    /// Single source of truth for employee input validation rules, shared by the
    /// presentation form (immediate UX feedback) and the application service (authoritative).
    /// </summary>
    public static class EmployeeValidator
    {
        public static bool TryValidate(string fullName, decimal baseSalary, out string error)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                error = "يجب إدخال اسم الموظف";
                return false;
            }

            if (baseSalary < 0)
            {
                error = "الراتب الأساسي يجب أن يكون صفر أو أكبر";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
