using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Screens.Admin;
using Prm.Client.Screens.Employee;
using Prm.Client.Screens.Manager;

namespace Prm.Client.Screens.Menus;

public static class MainMenuFactory
{
    public static IMenuScreen Create(ConsoleApp app) =>
        app.Session.User!.Role switch
        {
            "Admin" => new AdminMainMenuScreen(app),
            "Manager" => new ManagerMainMenuScreen(app),
            "Employee" => new EmployeeMainMenuScreen(app),
            _ => throw new InvalidOperationException($"Unsupported role: {app.Session.User.Role}")
        };
}
