namespace Prm.Application.DTOs.Employee;

public record EmployeeReminderResponse(
    bool HasReminder,
    DateOnly? WeekStart,
    string? Message);
