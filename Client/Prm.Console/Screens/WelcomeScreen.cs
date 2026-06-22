using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;
using Prm.Client.Screens.Menus;

namespace Prm.Client.Screens;

public sealed class WelcomeScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public WelcomeScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("PROJECT & RESOURCE MANAGEMENT TOOL");
        System.Console.WriteLine("        Learn & Code — Final Project");
        System.Console.WriteLine();
        System.Console.WriteLine("1. Login");
        System.Console.WriteLine("2. Exit");
        System.Console.WriteLine();

        if (ConsolePrompt.ReadMenuChoice(1, 2) == 2)
        {
            return MenuAction.ExitApp;
        }

        var username = ConsolePrompt.ReadLine("Username: ");
        var password = ConsolePrompt.ReadLine("Password: ", secret: true);

        try
        {
            var response = await _app.Api.LoginAsync(username, password, cancellationToken);
            _app.Session.ApplyLogin(response);
            _app.Api.SyncAuthorizationHeader();

            if (_app.Session.IsTemporaryPassword)
            {
                _app.Navigator.Push(new ChangePasswordScreen(_app));
                return MenuAction.None;
            }

            _app.Navigator.Push(MainMenuFactory.Create(_app));
            return MenuAction.None;
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"Login failed: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.None;
        }
    }
}

public sealed class ChangePasswordScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ChangePasswordScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("CHANGE PASSWORD");
        System.Console.WriteLine("You must set a new password to continue.");
        System.Console.WriteLine();
        BrdConsole.WriteRule();
        System.Console.WriteLine();

        var newPassword = ConsolePrompt.ReadLine("New Password        : ", secret: true);
        var confirmPassword = ConsolePrompt.ReadLine("Confirm Password    : ", secret: true);
        System.Console.WriteLine();
        BrdConsole.WriteRule();
        System.Console.WriteLine("[S] Save and Continue");
        System.Console.WriteLine();

        var choice = BrdConsole.ReadKeyChoice("Choice: ");
        if (choice != 'S')
        {
            return MenuAction.None;
        }

        try
        {
            await _app.Api.ChangePasswordAsync(newPassword, confirmPassword, cancellationToken);
            _app.Session.Logout();
            _app.Api.ClearAuthorizationHeader();
            _app.Navigator.ResetTo(new WelcomeScreen(_app));
            ScreenHelper.WriteSuccess("Password updated. Welcome!");
            ScreenHelper.Pause();
            return MenuAction.None;
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"Password change failed: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.None;
        }
    }
}
