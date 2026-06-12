using Prm.Client.Api;
using Prm.Client.Auth;
using Prm.Client.Navigation;

namespace Prm.Client.Core;

public sealed class ConsoleApp
{
    public SessionState Session { get; }
    public PrmApiClient Api { get; }
    public MenuNavigator Navigator { get; }

    public ConsoleApp(SessionState session, PrmApiClient api, MenuNavigator navigator)
    {
        Session = session;
        Api = api;
        Navigator = navigator;
    }
}
