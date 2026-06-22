using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Admin;

public sealed class AdminUsersMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUsersMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Manage Users", _app.Session);
        System.Console.WriteLine("1. Create User Account");
        System.Console.WriteLine("2. View All Users");
        System.Console.WriteLine("3. Reset User Password");
        System.Console.WriteLine("4. Deactivate User");
        System.Console.WriteLine("5. Back");
        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 5) switch
        {
            1 => Push(new AdminCreateUserScreen(_app)),
            2 => Push(new AdminViewUsersScreen(_app)),
            3 => Push(new AdminResetPasswordScreen(_app)),
            4 => Push(new AdminDeactivateUserScreen(_app)),
            5 => MenuAction.Back,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen s)
    {
        _app.Navigator.Push(s);
        return MenuAction.None;
    }
}

public sealed class AdminViewUsersScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminViewUsersScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("All Users", _app.Session);

        try
        {
            var users = await _app.Api.GetUsersAsync(cancellationToken);
            System.Console.WriteLine($"{"ID",4} {"Username",-18} {"Full Name",-22} {"Role",10} Active");
            foreach (var u in users)
            {
                System.Console.WriteLine($"{u.Id,4} {u.Username,-18} {u.FullName,-22} {u.Role,10} {(u.IsActive ? "Yes" : "No")}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("[R] Reactivate user     [B] Back");
            var choice = BrdConsole.ReadKeyChoice("Choice: ");
            if (choice == 'R' || choice == 'r')
            {
                _app.Navigator.Push(new AdminReactivateUserScreen(_app));
                return MenuAction.None;
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.Back;
        return MenuAction.Back;
    }
}

public sealed class AdminCreateUserScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminCreateUserScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Create User", _app.Session);
        var fullName = ConsolePrompt.ReadLine("Full name: ");
        var email = ConsolePrompt.ReadLine("Email: ");
        var username = ConsolePrompt.ReadLine("Username: ");
        var password = ConsolePrompt.ReadLine("Temporary password: ", secret: true);
        System.Console.WriteLine("Role: 1=Admin  2=Manager  3=Employee");
        var role = ConsolePrompt.ReadInt("Role: ", 1, 3) switch
        {
            1 => "Admin",
            2 => "Manager",
            3 => "Employee",
            _ => "Employee"
        };
        string? department = ConsolePrompt.ReadLine("Department: ");
        string? designation = ConsolePrompt.ReadLine("Designation: ");

        try
        {
            var user = await _app.Api.CreateUserAsync(
                new { fullName, email, username, temporaryPassword = password, role, department, designation },
                cancellationToken);
            ScreenHelper.WriteSuccess($"User '{user.Username}' created (ID {user.Id}).");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminResetPasswordScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminResetPasswordScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Reset Password", _app.Session);
        var id = ConsolePrompt.ReadInt("User ID: ");
        var pwd = ConsolePrompt.ReadLine("New temporary password: ", secret: true);

        try
        {
            var result = await _app.Api.ResetUserPasswordAsync(id, pwd, cancellationToken);
            ScreenHelper.WriteSuccess(result.Message);
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminDeactivateUserScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminDeactivateUserScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Deactivate User", _app.Session);
        var id = ConsolePrompt.ReadInt("User ID: ");

        if (ConsolePrompt.ReadYesNo("Yes, deactivate?"))
        {
            try
            {
                var result = await _app.Api.DeactivateUserAsync(id, cancellationToken);
                ScreenHelper.WriteSuccess(result.Message);
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminReactivateUserScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminReactivateUserScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Reactivate User", _app.Session);
        var id = ConsolePrompt.ReadInt("User ID: ");

        try
        {
            var result = await _app.Api.ReactivateUserAsync(id, cancellationToken);
            ScreenHelper.WriteSuccess(result.Message);
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminSettingsMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminSettingsMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("System Configuration", _app.Session);
        System.Console.WriteLine("1. View Settings");
        System.Console.WriteLine("2. Update LLM Provider");
        System.Console.WriteLine("3. Update LLM API Key");
        System.Console.WriteLine("4. Update Scheduler Interval");
        System.Console.WriteLine("5. Update Max Weekly Hours");
        System.Console.WriteLine("6. Back");
        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 6) switch
        {
            1 => Push(new AdminViewSettingsScreen(_app)),
            2 => Push(new AdminUpdateProviderScreen(_app)),
            3 => Push(new AdminUpdateApiKeyScreen(_app)),
            4 => Push(new AdminUpdateSchedulerScreen(_app)),
            5 => Push(new AdminUpdateMaxHoursScreen(_app)),
            6 => MenuAction.Back,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen s)
    {
        _app.Navigator.Push(s);
        return MenuAction.None;
    }
}

public sealed class AdminViewSettingsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminViewSettingsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("System Settings", _app.Session);

        try
        {
            var s = await _app.Api.GetSettingsAsync(cancellationToken);
            var apiKeyDisplay = string.IsNullOrEmpty(s.LlmApiKeyMasked)
                ? "(not configured)"
                : s.LlmApiKeyMasked;

            System.Console.WriteLine($"LLM Provider      : {s.LlmProvider}");
            System.Console.WriteLine($"LLM API Key       : {apiKeyDisplay}");
            if (string.Equals(s.LlmProvider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("                    (Set Ollama:BaseUrl in appsettings.json; API key from Admin if RequireApiKey=true)");
            }
            else if (string.IsNullOrEmpty(s.LlmApiKeyMasked))
            {
                System.Console.WriteLine("                    (Set API key below to enable live Gemini/Groq calls)");
            }

            System.Console.WriteLine($"Scheduler Interval: {s.SchedulerIntervalHours} hours");
            System.Console.WriteLine($"Max Weekly Hours  : {s.MaxWeeklyHours}");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminUpdateProviderScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateProviderScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Change LLM Provider", _app.Session);
        System.Console.WriteLine("1. Gemini  2. Groq  3. Ollama");
        var provider = ConsolePrompt.ReadInt("Provider: ", 1, 3) switch
        {
            1 => "Gemini",
            2 => "Groq",
            3 => "Ollama",
            _ => "Gemini"
        };

        try
        {
            await _app.Api.UpdateSettingsAsync(new { llmProvider = provider }, cancellationToken);
            ScreenHelper.WriteSuccess("Provider updated.");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminUpdateApiKeyScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateApiKeyScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Update LLM API Key", _app.Session);
        System.Console.WriteLine("Required for Gemini and Groq. Required for Ollama when RequireApiKey=true in appsettings.json.");
        System.Console.WriteLine("Paste-friendly input (visible while typing).");
        System.Console.WriteLine();

        var key = ConsolePrompt.ReadLine("API key: ");
        if (string.IsNullOrWhiteSpace(key))
        {
            System.Console.WriteLine("API key cannot be empty.");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        try
        {
            await _app.Api.UpdateSettingsAsync(new { llmApiKey = key.Trim() }, cancellationToken);
            ScreenHelper.WriteSuccess("API key updated. Use View Settings to confirm masked key.");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminUpdateSchedulerScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateSchedulerScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Update Scheduler Interval", _app.Session);
        var hours = ConsolePrompt.ReadInt("Interval (hours): ", min: 1);

        try
        {
            await _app.Api.UpdateSettingsAsync(new { schedulerIntervalHours = hours }, cancellationToken);
            ScreenHelper.WriteSuccess("Scheduler interval updated.");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminUpdateMaxHoursScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateMaxHoursScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Update Max Weekly Hours", _app.Session);
        var hours = ConsolePrompt.ReadInt("Max weekly hours: ", min: 1);

        try
        {
            await _app.Api.UpdateSettingsAsync(new { maxWeeklyHours = hours }, cancellationToken);
            ScreenHelper.WriteSuccess("Max weekly hours updated.");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}
