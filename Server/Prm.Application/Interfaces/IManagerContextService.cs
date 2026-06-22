namespace Prm.Application.Interfaces;

public interface IManagerContextService
{
    Task<ManagerContext> ResolveAsync(int managerUserId, CancellationToken cancellationToken = default);
}

public record ManagerContext(int ManagerUserId);
