using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin.Users;

namespace Prm.Application.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AdminUserProvisioningService _provisioningService;
    private readonly AdminUserCredentialService _credentialService;
    private readonly AdminUserLifecycleService _lifecycleService;

    public AdminUserService(
        AdminUserProvisioningService provisioningService,
        AdminUserCredentialService credentialService,
        AdminUserLifecycleService lifecycleService)
    {
        _provisioningService = provisioningService;
        _credentialService = credentialService;
        _lifecycleService = lifecycleService;
    }

    public Task<UserListItemDto> CreateAsync(CreateUserRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _provisioningService.CreateAsync(request, actingUserId, cancellationToken);

    public Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _lifecycleService.GetAllAsync(cancellationToken);

    public Task ResetPasswordAsync(int id, ResetPasswordRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _credentialService.ResetPasswordAsync(id, request, actingUserId, cancellationToken);

    public Task DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default) =>
        _lifecycleService.DeactivateAsync(id, actingUserId, cancellationToken);

    public Task ReactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default) =>
        _lifecycleService.ReactivateAsync(id, actingUserId, cancellationToken);
}
