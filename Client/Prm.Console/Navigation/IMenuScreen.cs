namespace Prm.Client.Navigation;

public enum MenuAction
{
    None,
    Back,
    Logout,
    ExitApp
}

public interface IMenuScreen
{
    Task<MenuAction> RunAsync(CancellationToken cancellationToken);
}
