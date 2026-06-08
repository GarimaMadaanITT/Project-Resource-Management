using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services.Manager;

public class ManagerContextService : IManagerContextService
{
    private readonly IEmployeeRepository _employees;

    public ManagerContextService(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public async Task<ManagerContext> ResolveAsync(int managerUserId, CancellationToken cancellationToken = default)
    {
        var managerEmployee = await _employees.GetByUserIdAsync(managerUserId, cancellationToken);

        if (managerEmployee is null)
        {
            throw new ForbiddenException(ErrorMessages.ManagerEmployeeProfileNotFound);
        }

        return new ManagerContext(managerUserId, managerEmployee.Id);
    }
}
