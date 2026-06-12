using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Models;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Admin;

public sealed class AdminProjectsMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminProjectsMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("MANAGE PROJECTS");
        System.Console.WriteLine("1. Create Project");
        System.Console.WriteLine("2. View All Projects");
        System.Console.WriteLine("3. Update Project Details");
        System.Console.WriteLine("4. Manage Milestones");
        System.Console.WriteLine("5. Back");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 5) switch
        {
            1 => Push(new AdminCreateProjectScreen(_app)),
            2 => Push(new AdminViewProjectsScreen(_app)),
            3 => Push(new AdminUpdateProjectScreen(_app)),
            4 => Push(new AdminManageMilestonesScreen(_app)),
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

public sealed class AdminViewProjectsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminViewProjectsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("ALL PROJECTS");

        try
        {
            var projects = await _app.Api.GetProjectsAsync(cancellationToken);
            System.Console.WriteLine(
                $"{"ID",4}  {"Name",-18} {"Manager",-14} {"End Date",-12} {"Status",-10} {"SP Done/Total",-13}");
            BrdConsole.WriteRule(78);
            foreach (var project in projects)
            {
                System.Console.WriteLine(
                    $"{project.Id,4}  {project.Name,-18} {project.ManagerName,-14} {ScreenHelper.FormatDate(project.EndDate),-12} {project.Status,-10} {project.StoryPointsDone,3} / {project.TotalStoryPoints}");
            }

            BrdConsole.WriteRule(78);
            System.Console.WriteLine();
            System.Console.WriteLine("[B] Back");
            System.Console.WriteLine();
            BrdConsole.ReadKeyChoice("Choice: ");
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.Back;
    }
}

public sealed class AdminCreateProjectScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminCreateProjectScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("CREATE PROJECT");
            System.Console.WriteLine("Project Name        : _");
            System.Console.WriteLine("Description         : _");
            System.Console.WriteLine("Start Date          : (DD-MM-YYYY) _");
            System.Console.WriteLine("End Date            : (DD-MM-YYYY) _");
            System.Console.WriteLine("Status              : (1) PLANNED   (2) ACTIVE   (3) ON_HOLD");
            System.Console.WriteLine("Assign Manager      : (Enter Manager ID) _");
            System.Console.WriteLine("Total Story Points  : _");
            System.Console.WriteLine();
            BrdConsole.WriteRule();
            System.Console.WriteLine("[S] Save     [B] Back");
            System.Console.WriteLine();

            var action = BrdConsole.ReadSaveOrBack();
            if (action == false)
            {
                return MenuAction.Back;
            }

            if (action != true)
            {
                continue;
            }

            System.Console.WriteLine();
            var name = ConsolePrompt.ReadLine("Project Name        : ");
            var description = ConsolePrompt.ReadLine("Description         : ");
            var start = ConsolePrompt.ReadDate("Start Date");
            var end = ConsolePrompt.ReadDate("End Date");
            System.Console.WriteLine("Status              : (1) PLANNED   (2) ACTIVE   (3) ON_HOLD");
            var status = ConsolePrompt.ReadInt("Enter choice        : ", 1, 3) switch
            {
                1 => "Planned",
                2 => "Active",
                3 => "OnHold",
                _ => "Planned"
            };
            var managerUserId = ConsolePrompt.ReadInt("Assign Manager      : ");
            var storyPoints = ConsolePrompt.ReadInt("Total Story Points  : ", min: 0);

            if (!BrdConsole.IsProjectDurationValid(start, end, storyPoints))
            {
                var days = end.DayNumber - start.DayNumber;
                System.Console.WriteLine(
                    $"Validation error: project duration ({days} days) must be greater than or equal to total story points ({storyPoints}).");
                ScreenHelper.Pause();
                continue;
            }

            try
            {
                var project = await _app.Api.CreateProjectAsync(
                    new { name, description, startDate = start, endDate = end, status, managerUserId, totalStoryPoints = storyPoints },
                    cancellationToken);
                ScreenHelper.WriteSuccess($"Project '{project.Name}' created (ID {project.Id}).");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
            }
        }
    }
}

public sealed class AdminUpdateProjectScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateProjectScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProjectListItemModel> projects;
        try
        {
            projects = await _app.Api.GetProjectsAsync(cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("UPDATE PROJECT DETAILS");
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        ScreenHelper.Clear();
        BrdConsole.WriteTitle("UPDATE PROJECT DETAILS");
        AdminProjectSelection.PrintProjectList(projects);
        System.Console.WriteLine();
        var id = ConsolePrompt.ReadInt("Enter Project ID: ");

        var selected = projects.FirstOrDefault(project => project.Id == id);
        if (selected is null)
        {
            System.Console.WriteLine("Project not found.");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("UPDATE PROJECT DETAILS");
            System.Console.WriteLine($"── {selected.Name} ───────────────────────────────");
            System.Console.WriteLine($"Project Name         : ");
            System.Console.WriteLine("Description          : ");
            System.Console.WriteLine("Start Date           : ");
            System.Console.WriteLine("End Date             : ");
            System.Console.WriteLine("Status               : (1) PLANNED   (2) ACTIVE   (3) ON_HOLD   (4) COMPLETED");
            System.Console.WriteLine("Assign Manager       : (Enter Manager ID)    ");
            System.Console.WriteLine($"Total Story Points   : ");
            BrdConsole.WriteRule();
            System.Console.WriteLine("[S] Save     [B] Back");
            System.Console.WriteLine();

            var action = BrdConsole.ReadSaveOrBack();
            if (action == false)
            {
                return MenuAction.Back;
            }

            if (action != true)
            {
                continue;
            }

            System.Console.WriteLine();
            var name = ConsolePrompt.ReadLine($"Project Name         : ");
            var description = ConsolePrompt.ReadLine("Description          : ");
            var start = ConsolePrompt.ReadDate("Start Date");
            var end = ConsolePrompt.ReadDate("End Date");
            System.Console.WriteLine("Status               : (1) PLANNED   (2) ACTIVE   (3) ON_HOLD   (4) COMPLETED");
            var status = ConsolePrompt.ReadInt("Enter choice         : ", 1, 4) switch
            {
                1 => "Planned",
                2 => "Active",
                3 => "OnHold",
                4 => "Completed",
                _ => selected.Status
            };
            var managerUserId = ConsolePrompt.ReadInt("Assign Manager       : ");
            var storyPoints = ConsolePrompt.ReadInt("Total Story Points   : ", min: 0);

            if (!BrdConsole.IsProjectDurationValid(start, end, storyPoints))
            {
                var days = end.DayNumber - start.DayNumber;
                System.Console.WriteLine(
                    $"Validation error: project duration ({days} days) must be greater than or equal to total story points ({storyPoints}).");
                ScreenHelper.Pause();
                continue;
            }

            try
            {
                await _app.Api.UpdateProjectAsync(
                    id,
                    new { name, description, startDate = start, endDate = end, status, managerUserId, totalStoryPoints = storyPoints },
                    cancellationToken);
                ScreenHelper.WriteSuccess("Project updated.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
            }
        }
    }
}

public sealed class AdminManageMilestonesScreen : IMenuScreen
{
    private readonly ConsoleApp _app;
    private int? _projectId;
    private string? _projectName;
    private int _totalStoryPoints;

    public AdminManageMilestonesScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        if (_projectId is null)
        {
            IReadOnlyList<ProjectListItemModel> projects;
            try
            {
                projects = await _app.Api.GetProjectsAsync(cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                ScreenHelper.Clear();
                BrdConsole.WriteTitle("MILESTONES");
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            ScreenHelper.Clear();
            BrdConsole.WriteTitle("MILESTONES");
            AdminProjectSelection.PrintProjectList(projects);
            System.Console.WriteLine();
            var id = ConsolePrompt.ReadInt("Enter Project ID: ");
            var selected = projects.FirstOrDefault(project => project.Id == id);
            if (selected is null)
            {
                System.Console.WriteLine("Project not found.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            _projectId = selected.Id;
            _projectName = selected.Name;
            _totalStoryPoints = selected.TotalStoryPoints;
        }

        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("MILESTONES");
            System.Console.WriteLine($"── {_projectName} ───────────────────────────────");

            IReadOnlyList<MilestoneModel> milestones;
            try
            {
                milestones = await _app.Api.GetMilestonesAsync(_projectId!.Value, cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            System.Console.WriteLine($"{"#",3}  {"Title",-20} {"Due Date",-12} {"Story Pts",10} {"Status",-12}");
            BrdConsole.WriteRule(62);
            for (var i = 0; i < milestones.Count; i++)
            {
                var milestone = milestones[i];
                System.Console.WriteLine(
                    $"{i + 1,3}. {milestone.Title,-20} {ScreenHelper.FormatDate(milestone.DueDate),-12} {milestone.StoryPoints,10} {milestone.Status,-12}");
            }

            BrdConsole.WriteRule(62);
            var completed = milestones.Where(m => m.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)).Sum(m => m.StoryPoints);
            var remaining = _totalStoryPoints - completed;
            System.Console.WriteLine($"Total: {_totalStoryPoints} SP   |   Completed: {completed} SP   |   Remaining: {remaining} SP");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Add Milestone");
            System.Console.WriteLine("2. Update Milestone Status");
            System.Console.WriteLine("3. Back");
            System.Console.WriteLine();

            switch (ConsolePrompt.ReadMenuChoice(1, 3))
            {
                case 1:
                    await AddMilestoneAsync(cancellationToken);
                    break;
                case 2:
                    await UpdateMilestoneStatusAsync(milestones, cancellationToken);
                    break;
                case 3:
                    return MenuAction.Back;
            }
        }
    }

    private async Task AddMilestoneAsync(CancellationToken cancellationToken)
    {
        System.Console.WriteLine();
        var title = ConsolePrompt.ReadLine("Milestone Title  : ");
        var due = ConsolePrompt.ReadDate("Due Date");
        var sp = ConsolePrompt.ReadInt("Story Points     : ", min: 1);

        try
        {
            var milestone = await _app.Api.AddMilestoneAsync(
                _projectId!.Value,
                new { title, dueDate = due, storyPoints = sp },
                cancellationToken);
            ScreenHelper.WriteSuccess($"Milestone '{milestone.Title}' added.");
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }
    }

    private async Task UpdateMilestoneStatusAsync(IReadOnlyList<MilestoneModel> milestones, CancellationToken cancellationToken)
    {
        if (milestones.Count == 0)
        {
            System.Console.WriteLine("No milestones to update.");
            ScreenHelper.Pause();
            return;
        }

        System.Console.WriteLine();
        var number = ConsolePrompt.ReadInt("Enter Milestone # : ", min: 1, max: milestones.Count);
        var milestone = milestones[number - 1];
        System.Console.WriteLine("New Status        : (1) NOT_STARTED   (2) IN_PROGRESS   (3) DONE");
        var status = ConsolePrompt.ReadInt("Enter choice      : ", 1, 3) switch
        {
            1 => "NotStarted",
            2 => "InProgress",
            3 => "Done",
            _ => milestone.Status
        };

        try
        {
            await _app.Api.UpdateMilestoneStatusAsync(_projectId!.Value, milestone.Id, status, cancellationToken);
            ScreenHelper.WriteSuccess("Milestone updated.");
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }
    }
}

internal static class AdminProjectSelection
{
    public static void PrintProjectList(IReadOnlyList<ProjectListItemModel> projects)
    {
        System.Console.WriteLine($"{"ID",4}  {"Name",-25} {"End Date",-12} {"Status",-10}");
        BrdConsole.WriteRule(55);
        foreach (var project in projects)
        {
            System.Console.WriteLine(
                $"{project.Id,4}  {project.Name,-25} {ScreenHelper.FormatDate(project.EndDate),-12} {project.Status,-10}");
        }
    }
}

public sealed class AdminAllocationsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminAllocationsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        int? employeeId = null;
        int? projectId = null;

        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("ALL ALLOCATIONS");
            System.Console.WriteLine($"{"Employee",-18} {"Project",-18} {"%",4} {"From",12} {"To",12}");
            BrdConsole.WriteRule(62);

            try
            {
                var rows = await _app.Api.GetAdminAllocationsAsync(employeeId, projectId, cancellationToken);
                foreach (var row in rows)
                {
                    System.Console.WriteLine(
                        $"{row.EmployeeName,-18} {row.ProjectName,-18} {row.UtilisationPercent,3}% {ScreenHelper.FormatDate(row.FromDate),12} {ScreenHelper.FormatDate(row.ToDate),12}");
                }

                BrdConsole.WriteRule(62);
                System.Console.WriteLine($"Total Active Allocations: {rows.Count}");
                System.Console.WriteLine();
                System.Console.WriteLine("[F] Filter by Employee / Project     [B] Back");
                System.Console.WriteLine();

                var choice = BrdConsole.ReadKeyChoice("Choice: ");
                if (choice == 'B')
                {
                    return MenuAction.Back;
                }

                if (choice == 'F')
                {
                    employeeId = null;
                    projectId = null;
                    if (ConsolePrompt.ReadYesNo("Filter by Employee ID?"))
                    {
                        employeeId = ConsolePrompt.ReadInt("Employee ID: ");
                    }

                    if (ConsolePrompt.ReadYesNo("Filter by Project ID?"))
                    {
                        projectId = ConsolePrompt.ReadInt("Project ID: ");
                    }

                    continue;
                }

                System.Console.WriteLine("Please enter F to filter or B to go back.");
                ScreenHelper.Pause();
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
        }
    }
}
