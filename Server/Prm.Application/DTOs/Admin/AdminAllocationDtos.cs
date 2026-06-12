namespace Prm.Application.DTOs.Admin;

public record AllocationListItemDto(
    string EmployeeName,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);
