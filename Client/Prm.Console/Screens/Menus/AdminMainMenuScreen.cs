using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;
using Prm.Client.Screens.Admin;

namespace Prm.Client.Screens.Menus;

public sealed class AdminMainMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminMainMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Admin Main Menu", _app.Session);
        System.Console.WriteLine("1. Manage Employees");
        System.Console.WriteLine("2. Manage Projects");
        System.Console.WriteLine("3. View All Allocations");
        System.Console.WriteLine("4. Manage Users");
        System.Console.WriteLine("5. System Configuration");
        System.Console.WriteLine("6. View Audit Logs");
        System.Console.WriteLine("7. Logout");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 7) switch
        {
            1 => Push(new AdminEmployeesMenuScreen(_app)),
            2 => Push(new AdminProjectsMenuScreen(_app)),
            3 => RunSync(new AdminAllocationsScreen(_app)),
            4 => Push(new AdminUsersMenuScreen(_app)),
            5 => Push(new AdminSettingsMenuScreen(_app)),
            6 => RunSync(new AdminAuditLogsScreen(_app)),
            7 => MenuAction.Logout,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen screen)
    {
        _app.Navigator.Push(screen);
        return MenuAction.None;
    }

    private MenuAction RunSync(IMenuScreen screen)
    {
        _app.Navigator.Push(screen);
        return MenuAction.None;
    }
}
