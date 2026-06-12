using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Manager;

public sealed class ManagerDashboardScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerDashboardScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Resource Dashboard", _app.Session);

        try
        {
            var dash = await _app.Api.GetManagerDashboardAsync(cancellationToken);
            PrintGroup("BENCH", dash.Bench);
            PrintGroup("PARTIAL", dash.Partial);
            PrintGroup("FULL", dash.Full);

            System.Console.WriteLine();
            System.Console.WriteLine($"Summary: Bench={dash.BenchCount}, Partial={dash.PartialCount}, Full={dash.FullCount}");
            System.Console.WriteLine();
            System.Console.WriteLine("[D] Drill into employee  [B] Back");
            var choice = ConsolePrompt.ReadLine("Choice: ").Trim();
            if (choice.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return MenuAction.Back;
            }

            if (choice.Equals("D", StringComparison.OrdinalIgnoreCase))
            {
                var id = ConsolePrompt.ReadInt("Employee ID: ");
                var detail = await _app.Api.GetManagerEmployeeDetailAsync(id, cancellationToken);
                System.Console.WriteLine();
                System.Console.WriteLine($"{detail.Name} | {detail.Department} | {detail.Status} | {detail.UtilisationPercent}% util");
                System.Console.WriteLine($"Skills: {string.Join(", ", detail.ProfileSkills)}");
                System.Console.WriteLine("Allocations:");
                foreach (var a in detail.ActiveAllocations)
                {
                    System.Console.WriteLine($"  {a.ProjectName} {a.UtilisationPercent}% ({ScreenHelper.FormatDate(a.FromDate)} - {ScreenHelper.FormatDate(a.ToDate)})");
                }

                if (detail.RecentActivityTags.Count > 0)
                {
                    System.Console.WriteLine($"Recent tags: {string.Join(", ", detail.RecentActivityTags)}");
                }

                ScreenHelper.Pause();
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }

    private static void PrintGroup(string title, IReadOnlyList<Models.ManagerDashboardEmployeeModel> employees)
    {
        System.Console.WriteLine($"--- {title} ---");
        if (employees.Count == 0)
        {
            System.Console.WriteLine("  (none)");
            return;
        }

        foreach (var e in employees)
        {
            System.Console.WriteLine(
                $"  {e.Id,3}  {e.Name,-20} {e.Department,-12} {e.UtilisationPercent,3}% util  {e.AvailabilityPercent,3}% free  {e.SkillsSummary}");
        }

        System.Console.WriteLine();
    }
}

public sealed class ManagerAllocationMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerAllocationMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Allocate Resource", _app.Session);
        System.Console.WriteLine("1. Find resource using AI (recommended)");
        System.Console.WriteLine("2. Allocate directly");
        System.Console.WriteLine("3. End an existing allocation");
        System.Console.WriteLine("4. Back");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 4) switch
        {
            1 => Push(new ManagerAiSkillMatchScreen(_app)),
            2 => Push(new ManagerCreateAllocationScreen(_app)),
            3 => Push(new ManagerEndAllocationScreen(_app)),
            4 => MenuAction.Back,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen screen)
    {
        _app.Navigator.Push(screen);
        return MenuAction.None;
    }
}

public sealed class ManagerCreateAllocationScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerCreateAllocationScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Create Allocation", _app.Session);

        try
        {
            var projectId = ConsolePrompt.ReadInt("Project ID: ");
            var employeeId = ConsolePrompt.ReadInt("Employee ID: ");
            var util = ConsolePrompt.ReadInt("Utilisation %: ", min: 1, max: 100);
            var from = ConsolePrompt.ReadDate("From");
            var to = ConsolePrompt.ReadDate("To");

            var result = await _app.Api.CreateManagerAllocationAsync(
                new { projectId, employeeId, utilisationPercent = util, fromDate = from, toDate = to },
                cancellationToken);

            ScreenHelper.WriteSuccess($"Allocation created (ID {result.Id}). Employee status updated.");
            System.Console.WriteLine($"{result.EmployeeName} → {result.ProjectName} @ {result.UtilisationPercent}%");
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.Back;
    }
}

public sealed class ManagerEndAllocationScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerEndAllocationScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("End Allocation", _app.Session);
        System.Console.WriteLine("Enter allocation ID (shown when allocation was created).");
        System.Console.WriteLine();

        try
        {
            var id = ConsolePrompt.ReadInt("Allocation ID: ");
            DateOnly? endDate = null;
            if (ConsolePrompt.ReadYesNo("End on a specific date?"))
            {
                endDate = ConsolePrompt.ReadDate("End date");
            }

            if (ConsolePrompt.ReadYesNo("Confirm end allocation?"))
            {
                var result = await _app.Api.EndManagerAllocationAsync(id, endDate, cancellationToken);
                ScreenHelper.WriteSuccess(result.Message);
                System.Console.WriteLine($"Employee status: {result.EmployeeStatus}");
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class ManagerProjectsMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerProjectsMenuScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("My Projects", _app.Session);

        try
        {
            var projects = await _app.Api.GetManagerProjectsAsync(cancellationToken);
            if (projects.Count == 0)
            {
                System.Console.WriteLine("No projects.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            for (var i = 0; i < projects.Count; i++)
            {
                var p = projects[i];
                System.Console.WriteLine($"{i + 1,2}. {p.Name,-25} End: {ScreenHelper.FormatDate(p.EndDate)}  Health: {ScreenHelper.FormatHealthStatus(p.HealthStatus)}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Enter project number for details, or [B] Back");
            var input = ConsolePrompt.ReadLine("Choice: ").Trim();
            if (input.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return MenuAction.Back;
            }

            if (int.TryParse(input, out var sel) && sel >= 1 && sel <= projects.Count)
            {
                var detail = await _app.Api.GetManagerProjectDetailAsync(projects[sel - 1].Id, cancellationToken);
                System.Console.WriteLine();
                System.Console.WriteLine($"{detail.Name} — {ScreenHelper.FormatHealthStatus(detail.HealthStatus)}");
                System.Console.WriteLine(detail.Description);
                System.Console.WriteLine("Risk flags:");
                foreach (var flag in detail.RiskFlags.Where(f => f.IsRisk))
                {
                    System.Console.WriteLine($"  • {flag.Message}");
                }

                System.Console.WriteLine("Milestones:");
                foreach (var m in detail.Milestones)
                {
                    var overdue = m.IsOverdue ? " (OVERDUE)" : string.Empty;
                    System.Console.WriteLine($"  {m.Title} — {ScreenHelper.FormatDate(m.DueDate)} — {m.Status}{overdue}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine("[A] Get AI Risk Summary     [B] Back");
                var action = ConsolePrompt.ReadLine("Choice: ").Trim();
                if (action.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    _app.Navigator.Push(new ManagerAiRiskSummaryScreen(_app, projects[sel - 1].Id));
                }
                else
                {
                    ScreenHelper.Pause();
                }
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }
}

public sealed class ManagerTimesheetsMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerTimesheetsMenuScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Team Timesheets", _app.Session);

        DateOnly? week = null;
        if (ConsolePrompt.ReadYesNo("Filter by week start?"))
        {
            week = ConsolePrompt.ReadDate("Week start");
        }

        try
        {
            var data = await _app.Api.GetTeamTimesheetsAsync(week, cancellationToken);
            System.Console.WriteLine($"Week: {ScreenHelper.FormatDate(data.WeekStart)}");
            System.Console.WriteLine($"{"Employee",-20} {"Project",-20} {"Hrs",5} {"Status",10}");
            System.Console.WriteLine(new string('-', 58));
            foreach (var row in data.Rows)
            {
                System.Console.WriteLine($"{row.EmployeeName,-20} {row.ProjectName,-20} {row.Hours,5:0.##} {row.Status,10}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("[V] View employee detail  [B] Back");
            var choice = ConsolePrompt.ReadLine("Choice: ").Trim();
            if (choice.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return MenuAction.Back;
            }

            if (choice.Equals("V", StringComparison.OrdinalIgnoreCase))
            {
                var empId = ConsolePrompt.ReadInt("Employee ID: ");
                var detail = await _app.Api.GetEmployeeTimesheetDetailAsync(empId, data.WeekStart, cancellationToken);
                System.Console.WriteLine();
                System.Console.WriteLine($"{detail.EmployeeName} — {detail.Status}");
                foreach (var entry in detail.Entries)
                {
                    System.Console.WriteLine($"  {entry.ProjectName}: {entry.Hours} hrs — {string.Join(", ", entry.ActivityTags)}");
                }

                ScreenHelper.Pause();
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }
}
