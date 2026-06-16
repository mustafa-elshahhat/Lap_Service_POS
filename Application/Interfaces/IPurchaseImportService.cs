using AlJohary.ServiceHub.Application.DTOs;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IPurchaseImportService
    {
        ExcelImportResult Import(string filePath);
    }
}
