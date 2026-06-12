using Prm.Client.Api;
using Prm.Client.Auth;
using Prm.Client.Rendering;

namespace Prm.Client.Navigation;

public sealed class MenuNavigator
{
    private readonly Stack<IMenuScreen> _stack = new();
    private readonly SessionState _session;
    private readonly PrmApiClient _api;

    public MenuNavigator(SessionState session, PrmApiClient api)
    {
        _session = session;
        _api = api;
    }

    public void ResetTo(IMenuScreen screen)
    {
        _stack.Clear();
        _stack.Push(screen);
    }

    public void Push(IMenuScreen screen) => _stack.Push(screen);

    public void PerformLogout()
    {
        _session.Logout();
        _api.ClearAuthorizationHeader();
        _stack.Clear();
    }

    public async Task RunAsync(IMenuScreen welcomeScreen, CancellationToken cancellationToken)
    {
        ResetTo(welcomeScreen);

        while (_stack.Count > 0)
        {
            MenuAction action;
            try
            {
                action = await _stack.Peek().RunAsync(cancellationToken);
            }
            catch (SessionExpiredException ex)
            {
                PerformLogout();
                System.Console.WriteLine();
                System.Console.WriteLine(ex.Message);
                ScreenHelper.Pause();
                ResetTo(welcomeScreen);
                continue;
            }

            switch (action)
            {
                case MenuAction.Back when _stack.Count > 1:
                    _stack.Pop();
                    break;
                case MenuAction.Logout:
                    PerformLogout();
                    ScreenHelper.WriteSuccess("Logged out successfully.");
                    ScreenHelper.Pause();
                    ResetTo(welcomeScreen);
                    break;
                case MenuAction.ExitApp:
                    _stack.Clear();
                    break;
            }
        }
    }
}
