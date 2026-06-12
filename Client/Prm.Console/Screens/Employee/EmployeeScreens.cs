using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Employee;

public sealed class EmployeeAllocationsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public EmployeeAllocationsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("My Allocations", _app.Session);

        try
        {
            var data = await _app.Api.GetMyAllocationsAsync(cancellationToken);
            if (data.Allocations.Count == 0)
            {
                System.Console.WriteLine("No active allocations.");
            }
            else
            {
                System.Console.WriteLine($"{"Project",-25} {"Util%",5} {"From",12} {"To",12} {"Status",8}");
                System.Console.WriteLine(new string('-', 65));
                foreach (var allocation in data.Allocations)
                {
                    System.Console.WriteLine(
                        $"{allocation.ProjectName,-25} {allocation.UtilisationPercent,5} {ScreenHelper.FormatDate(allocation.FromDate),12} {ScreenHelper.FormatDate(allocation.ToDate),12} {allocation.Status,8}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Total utilisation: {data.TotalUtilisationPercent}%");
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("[B] Back");
        if (ConsolePrompt.ReadLine("Choice: ").Trim().Equals("B", StringComparison.OrdinalIgnoreCase))
        {
            return MenuAction.Back;
        }

        return MenuAction.None;
    }
}

public sealed class EmployeeTimesheetsMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public EmployeeTimesheetsMenuScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("My Timesheets (Last 12 Weeks)", _app.Session);

        try
        {
            var data = await _app.Api.GetMyTimesheetsAsync(cancellationToken);
            if (data.Weeks.Count == 0)
            {
                System.Console.WriteLine("No timesheet history.");
                System.Console.WriteLine();
                System.Console.WriteLine("[B] Back");
                if (ConsolePrompt.ReadLine("Choice: ").Trim().Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    return MenuAction.Back;
                }

                return MenuAction.None;
            }

            for (var i = 0; i < data.Weeks.Count; i++)
            {
                var week = data.Weeks[i];
                System.Console.WriteLine(
                    $"{i + 1,2}. Week {ScreenHelper.FormatDate(week.WeekStart)} — {week.TotalHours,5:0.##} hrs — {week.Status}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Enter week number to view details, or [B] Back");
            var input = ConsolePrompt.ReadLine("Choice: ").Trim();
            if (input.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return MenuAction.Back;
            }

            if (int.TryParse(input, out var selected) && selected >= 1 && selected <= data.Weeks.Count)
            {
                var detail = await _app.Api.GetTimesheetWeekAsync(data.Weeks[selected - 1].WeekStart, cancellationToken);
                System.Console.WriteLine();
                System.Console.WriteLine($"Week: {ScreenHelper.FormatDate(detail.WeekStart)} | Status: {detail.Status} | Total: {detail.TotalHours} hrs");
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

public sealed class EmployeeSubmitTimesheetScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public EmployeeSubmitTimesheetScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Submit Timesheet", _app.Session);

        try
        {
            var allocations = await _app.Api.GetMyAllocationsAsync(cancellationToken);
            if (allocations.Allocations.Count == 0)
            {
                System.Console.WriteLine("No active allocations — cannot submit a timesheet.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            var tags = await _app.Api.GetActivityTagsAsync(cancellationToken);
            var weekStart = ConsolePrompt.ReadDate("Week start (Monday)");

            if (weekStart.DayOfWeek != DayOfWeek.Monday)
            {
                System.Console.WriteLine("Week start must be a Monday.");
                ScreenHelper.Pause();
                return MenuAction.None;
            }

            var entries = new List<object>();
            System.Console.WriteLine();
            System.Console.WriteLine("Active projects:");
            for (var i = 0; i < allocations.Allocations.Count; i++)
            {
                var a = allocations.Allocations[i];
                System.Console.WriteLine($"{i + 1}. {a.ProjectName} (ID {a.ProjectId}) — {a.UtilisationPercent}%");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Enter entries (project number, 0 to finish):");

            while (true)
            {
                var projectNum = ConsolePrompt.ReadInt("Project number (0 = done): ", min: 0, max: allocations.Allocations.Count);
                if (projectNum == 0)
                {
                    break;
                }

                var project = allocations.Allocations[projectNum - 1];
                var hours = ConsolePrompt.ReadDecimal("Hours: ");
                if (hours == 0)
                {
                    continue;
                }

                System.Console.WriteLine("Activity tags (enter numbers, comma-separated):");
                for (var t = 0; t < tags.PredefinedTags.Count; t++)
                {
                    System.Console.WriteLine($"  {t + 1}. {tags.PredefinedTags[t]}");
                }

                var tagInput = ConsolePrompt.ReadLine("Tag numbers: ");
                var selectedTags = tagInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var n) ? n : 0)
                    .Where(n => n >= 1 && n <= tags.PredefinedTags.Count)
                    .Select(n => tags.PredefinedTags[n - 1])
                    .ToList();

                if (selectedTags.Count == 0)
                {
                    System.Console.WriteLine("At least one activity tag is required when hours > 0.");
                    continue;
                }

                if (selectedTags.Contains("Other"))
                {
                    var custom = ConsolePrompt.ReadLine("Custom activity (Other): ");
                    selectedTags.Remove("Other");
                    if (!string.IsNullOrWhiteSpace(custom))
                    {
                        selectedTags.Add(custom);
                    }
                }

                entries.Add(new { projectId = project.ProjectId, hours, activityTags = selectedTags });
            }

            if (entries.Count == 0)
            {
                System.Console.WriteLine("No entries to submit.");
                ScreenHelper.Pause();
                return MenuAction.None;
            }

            var result = await _app.Api.SubmitTimesheetAsync(new { weekStart, entries }, cancellationToken);
            ScreenHelper.WriteSuccess(result.Message);
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
