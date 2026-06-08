using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);
}
