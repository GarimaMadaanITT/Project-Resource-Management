using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Manager;

public sealed class ManagerAiAssistantMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public ManagerAiAssistantMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("AI Assistant", _app.Session);
        System.Console.WriteLine("1. Skill Match — find employees for a requirement");
        System.Console.WriteLine("2. Risk Summary — project health analysis");
        System.Console.WriteLine("3. Team Builder — build a multi-role team from one prompt");
        System.Console.WriteLine("4. Back");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 4) switch
        {
            1 => Push(new ManagerAiSkillMatchScreen(_app)),
            2 => Push(new ManagerAiRiskSummaryScreen(_app)),
            3 => Push(new ManagerAiTeamBuilderScreen(_app)),
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

public sealed class ManagerAiSkillMatchScreen : IMenuScreen
{
    private readonly ConsoleApp _app;
    private readonly int? _preselectedProjectId;

    public ManagerAiSkillMatchScreen(ConsoleApp app, int? preselectedProjectId = null)
    {
        _app = app;
        _preselectedProjectId = preselectedProjectId;
    }

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Skill Match", _app.Session);

        try
        {
            var projectId = _preselectedProjectId ?? await SelectProjectAsync(cancellationToken);
            if (projectId is null)
            {
                return MenuAction.Back;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Describe your project requirement in plain English:");
            var requirement = ConsolePrompt.ReadLine("> ");
            if (string.IsNullOrWhiteSpace(requirement))
            {
                System.Console.WriteLine("Requirement cannot be empty.");
                ScreenHelper.Pause();
                return MenuAction.None;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Searching... (AI matching in progress)");
            var result = await _app.Api.SkillMatchAsync(projectId.Value, requirement, cancellationToken);

            System.Console.WriteLine();
            System.Console.WriteLine($"AI-MATCHED RESULTS — {result.ProjectName}");
            System.Console.WriteLine(new string('-', 58));
            if (result.Matches.Count == 0)
            {
                System.Console.WriteLine("No matches returned.");
            }
            else
            {
                for (var i = 0; i < result.Matches.Count; i++)
                {
                    var match = result.Matches[i];
                    System.Console.WriteLine(
                        $"{i + 1}. {match.EmployeeName} (ID {match.EmployeeId}) — {match.UtilisationPercent}% util, {match.AvailabilityPercent}% free");
                    System.Console.WriteLine($"   {match.Reason}");
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine(result.Disclaimer);
            if (result.UsedFallbackProvider)
            {
                System.Console.WriteLine("(Used offline ranking — configure LLM API key in Admin settings for live AI.)");
            }

            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }

    internal static async Task<int?> SelectProjectAsync(ConsoleApp app, CancellationToken cancellationToken)
    {
        var projects = await app.Api.GetManagerProjectsAsync(cancellationToken);
        if (projects.Count == 0)
        {
            System.Console.WriteLine("No projects available.");
            ScreenHelper.Pause();
            return null;
        }

        for (var i = 0; i < projects.Count; i++)
        {
            var project = projects[i];
            System.Console.WriteLine($"{i + 1,2}. {project.Name,-25} Health: {ScreenHelper.FormatHealthStatus(project.HealthStatus)}");
        }

        System.Console.WriteLine();
        var input = ConsolePrompt.ReadLine("Select project number (or B to cancel): ").Trim();
        if (input.Equals("B", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (int.TryParse(input, out var selected) && selected >= 1 && selected <= projects.Count)
        {
            return projects[selected - 1].Id;
        }

        System.Console.WriteLine("Invalid selection.");
        ScreenHelper.Pause();
        return null;
    }

    private Task<int?> SelectProjectAsync(CancellationToken cancellationToken) =>
        SelectProjectAsync(_app, cancellationToken);
}

public sealed class ManagerAiRiskSummaryScreen : IMenuScreen
{
    private readonly ConsoleApp _app;
    private readonly int? _preselectedProjectId;

    public ManagerAiRiskSummaryScreen(ConsoleApp app, int? preselectedProjectId = null)
    {
        _app = app;
        _preselectedProjectId = preselectedProjectId;
    }

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("AI Risk Summary", _app.Session);

        try
        {
            var projectId = _preselectedProjectId ?? await ManagerAiSkillMatchScreen.SelectProjectAsync(_app, cancellationToken);
            if (projectId is null)
            {
                return MenuAction.Back;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Generating AI summary...");
            var result = await _app.Api.GetRiskSummaryAsync(projectId.Value, cancellationToken);

            System.Console.WriteLine();
            System.Console.WriteLine($"── AI Risk Summary — {result.ProjectName} ──");
            System.Console.WriteLine();
            System.Console.WriteLine($"\"{result.Summary}\"");
            System.Console.WriteLine();
            System.Console.WriteLine(result.Disclaimer);
            if (result.UsedFallbackProvider)
            {
                System.Console.WriteLine("(Used offline summary — configure LLM API key in Admin settings for live AI.)");
            }

            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }
}
