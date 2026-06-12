namespace Prm.Application.Validation;

public static class EntityGuard
{
    public static T EnsureFound<T>(T? entity, string notFoundMessage) where T : class
    {
        if (entity is null)
        {
            throw new KeyNotFoundException(notFoundMessage);
        }

        return entity;
    }
}
