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
                return MenuAction.None;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Searching... (AI team matching in progress)");
            var result = await _app.Api.TeamBuilderAsync(requirement, cancellationToken);
            DisplayResults(result);
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }

        return MenuAction.None;
    }

    private static string? PromptForRequirement()
    {
        System.Console.WriteLine("Describe your team requirement in plain English");
        System.Console.WriteLine("(include every role, skills, and proficiency).");
        System.Console.WriteLine("Press [E] then Enter to load the banking portal example, or type your requirement:");
        var input = ConsolePrompt.ReadLine("> ");
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

    private static void DisplayResults(TeamBuilderResponseModel result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("── Team Builder Results ──────────────────────");
        System.Console.WriteLine($"Candidates considered: {result.CandidatesConsidered} " +
                                 $"(fully benched: {result.AssignableCandidates})");
        System.Console.WriteLine();

        foreach (var role in result.Roles)
        {
            System.Console.WriteLine($"Role: {role.RoleTitle}");
            var skillsText = role.RequiredSkills.Count == 0
                ? "none"
                : string.Join(", ", role.RequiredSkills.Select(
                    skill => skill.MinProficiency == "ANY"
                        ? skill.SkillName
                        : $"{skill.SkillName} ({skill.MinProficiency})"));
            System.Console.WriteLine($"  Required: {skillsText}");
            System.Console.WriteLine($"  Status:   {role.Status}");

            if (role.BenchMatches.Count > 0)
            {
                System.Console.WriteLine("  Benched matches (ranked):");
                for (var i = 0; i < role.BenchMatches.Count; i++)
                {
                    var bench = role.BenchMatches[i];
                    var skills = bench.MatchedSkills.Count == 0
                        ? "none"
                        : string.Join(", ", bench.MatchedSkills);
                    System.Console.WriteLine(
                        $"    {i + 1}. {bench.EmployeeName} (UserId {bench.UserId}, ProfileId {bench.EmployeeId}) — score {bench.MatchScore}, skills: {skills}");
                }
            }

            if (role.Status == "FILLED")
            {
                System.Console.WriteLine($"  Match:    {role.AssignedEmployeeName} (score {role.MatchScore})");
                if (!string.IsNullOrWhiteSpace(role.Reason))
                {
                    System.Console.WriteLine($"  Reason:   {role.Reason}");
                }
            }
            else if (role.Gap is not null)
            {
                System.Console.WriteLine($"  Why:      {role.Gap.ReasonType}");
                System.Console.WriteLine($"  Note:     {role.Gap.Message}");
                if (!string.IsNullOrWhiteSpace(role.Gap.AlternativeEmployeeName))
                {
                    var availableFrom = string.IsNullOrWhiteSpace(role.Gap.AvailableFromDate)
                        ? "unknown date"
                        : role.Gap.AvailableFromDate;
                    System.Console.WriteLine(
                        $"            {role.Gap.AlternativeEmployeeName} may be available from {availableFrom}.");
                }
            }

            System.Console.WriteLine();
        }

        System.Console.WriteLine(result.Disclaimer);
        if (result.UsedFallbackProvider)
        {
            System.Console.WriteLine("(Used offline matching — configure LLM provider in Admin settings for live AI.)");
        }
    }
}
