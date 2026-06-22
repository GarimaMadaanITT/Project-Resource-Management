using Prm.Client.Models;

namespace Prm.Client.Auth;

public sealed class SessionState
{
    public string? Token { get; private set; }
    public AuthenticatedUserModel? User { get; private set; }
    public bool IsTemporaryPassword { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && User is not null;

    public void ApplyLogin(LoginResponse response)
    {
        Token = response.Token;
        User = response.User;
        IsTemporaryPassword = response.IsTemporaryPassword;
    }

    public void ApplyPasswordChange(ChangePasswordResponse response)
    {
        IsTemporaryPassword = response.IsTemporaryPassword;
        if (!string.IsNullOrWhiteSpace(response.Token))
        {
            Token = response.Token;
        }
    }

    public void Logout()
    {
        Token = null;
        User = null;
        IsTemporaryPassword = false;
    }
}
