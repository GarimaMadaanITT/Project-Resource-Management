using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services.Employees;

public class EmployeeContextService : IEmployeeContextService
{
    private readonly IEmployeeRepository _employees;

    public EmployeeContextService(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public async Task<EmployeeContext> ResolveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByUserIdAsync(userId, cancellationToken);

        if (employee is null)
        {
            throw new ForbiddenException(ErrorMessages.EmployeeProfileNotFound);
        }

        EmployeeGuard.EnsureActive(employee);

        return new EmployeeContext(userId, employee.Id, employee.User.FullName);
    }
}
