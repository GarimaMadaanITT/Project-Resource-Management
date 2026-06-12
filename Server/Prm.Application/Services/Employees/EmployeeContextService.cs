using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services.Employees;

public class EmployeeContextService : IEmployeeContextService
{
    private readonly IResourceProfileRepository _resourceProfiles;

    public EmployeeContextService(IResourceProfileRepository resourceProfiles)
    {
        _resourceProfiles = resourceProfiles;
    }

    public async Task<EmployeeContext> ResolveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resourceProfile = await _resourceProfiles.GetByUserIdAsync(userId, cancellationToken);

        if (resourceProfile is null)
        {
            throw new ForbiddenException(ErrorMessages.EmployeeProfileNotFound);
        }

        EmployeeGuard.EnsureActive(resourceProfile);

        return new EmployeeContext(userId, resourceProfile.Id, resourceProfile.User.FullName);
    }
}
