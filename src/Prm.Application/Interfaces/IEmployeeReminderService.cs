using Prm.Application.DTOs.Employee;

namespace Prm.Application.Interfaces;

public interface IEmployeeReminderService
{
    Task<EmployeeReminderResponse> GetReminderAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
