using Prm.Application.Interfaces;

namespace Prm.Application.Services.Manager;

public class ManagerContextService : IManagerContextService
{
    public Task<ManagerContext> ResolveAsync(int managerUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagerContext(managerUserId));
}
