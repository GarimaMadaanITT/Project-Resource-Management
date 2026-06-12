using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;
using Prm.Client.Screens.Employee;
using Prm.Client.Screens.Manager;

namespace Prm.Client.Screens.Menus;

public sealed class ManagerMainMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerMainMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Manager Main Menu", _app.Session);
        System.Console.WriteLine("1. Resource Dashboard");
        System.Console.WriteLine("2. Allocate Resource");
        System.Console.WriteLine("3. My Projects");
        System.Console.WriteLine("4. Timesheets");
        System.Console.WriteLine("5. AI Assistant");
        System.Console.WriteLine("6. Logout");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 6) switch
        {
            1 => Push(new ManagerDashboardScreen(_app)),
            2 => Push(new ManagerAllocationMenuScreen(_app)),
            3 => Push(new ManagerProjectsMenuScreen(_app)),
            4 => Push(new ManagerTimesheetsMenuScreen(_app)),
            5 => Push(new ManagerAiAssistantMenuScreen(_app)),
            6 => MenuAction.Logout,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen screen)
    {
        _app.Navigator.Push(screen);
        return MenuAction.None;
    }
}

public sealed class EmployeeMainMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public EmployeeMainMenuScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Employee Main Menu", _app.Session);

        try
        {
            var reminder = await _app.Api.GetReminderAsync(cancellationToken);
            if (reminder.HasReminder && reminder.Message is not null)
            {
                System.Console.WriteLine($"⚠  {reminder.Message}");
                System.Console.WriteLine();
            }
        }
        catch (ApiRequestException)
        {
            // non-fatal on menu load
        }

        System.Console.WriteLine("1. Submit Timesheet");
        System.Console.WriteLine("2. View My Timesheets");
        System.Console.WriteLine("3. View My Allocations");
        System.Console.WriteLine("4. Logout");
        System.Console.WriteLine();

        return ConsolePrompt.ReadMenuChoice(1, 4) switch
        {
            1 => Push(new EmployeeSubmitTimesheetScreen(_app)),
            2 => Push(new EmployeeTimesheetsMenuScreen(_app)),
            3 => Push(new EmployeeAllocationsScreen(_app)),
            4 => MenuAction.Logout,
            _ => MenuAction.None
        };
    }

    private MenuAction Push(IMenuScreen screen)
    {
        _app.Navigator.Push(screen);
        return MenuAction.None;
    }
}
