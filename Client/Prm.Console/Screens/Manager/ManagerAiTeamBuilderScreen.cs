using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Models;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Manager;

public sealed class ManagerAiTeamBuilderScreen : IMenuScreen
{
    private const string BankingPortalExample =
        "For a new banking portal we need a Senior Java Developer with advanced Java and intermediate Spring, " +
        "a DevOps Engineer with intermediate Docker and beginner Kubernetes, " +
        "and a QA Tester with intermediate manual testing and beginner Selenium.";

    private readonly ConsoleApp _app;

    public ManagerAiTeamBuilderScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        ScreenHelper.WriteHeader("Team Builder with Skill Match", _app.Session);

        try
        {
            var requirement = PromptForRequirement();
            if (requirement is null)
            {
                return MenuAction.Back;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Searching... (AI team matching in progress)");
            var result = await _app.Api.TeamBuilderAsync(requirement, cancellationToken);
            AiResultsFormatter.WriteTeamBuilderResults(result);
            if (result.UsedFallbackProvider)
            {
                System.Console.WriteLine("(Used offline matching — configure LLM provider in Admin settings for live AI.)");
            }
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.Back;
    }

    private static string? PromptForRequirement()
    {
        System.Console.WriteLine("Describe your team requirement in plain English");
        System.Console.WriteLine("(include every role, skills, and proficiency).");
        System.Console.WriteLine("Press [E] then Enter to load the banking portal example, [B] to go back, or type your requirement:");
        var input = ConsolePrompt.ReadLine("> ");
        if (string.Equals(input, "B", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(input, "E", StringComparison.OrdinalIgnoreCase))
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Example loaded:");
            System.Console.WriteLine(BankingPortalExample);
            return BankingPortalExample;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            System.Console.WriteLine("Requirement cannot be empty.");
            ScreenHelper.Pause();
            return null;
        }

        return input.Trim();
    }
}
