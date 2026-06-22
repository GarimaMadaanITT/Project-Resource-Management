using Prm.Client.Models;

namespace Prm.Client.Rendering;

public static class AiResultsFormatter
{
    private const int BoxWidth = 40;

    public static void WriteTeamBuilderResults(TeamBuilderResponseModel result)
    {
        WriteBanner("AI TEAM BUILDER RESULTS");

        System.Console.WriteLine("Request:");
        WriteWrapped(result.Requirement);
        System.Console.WriteLine();

        System.Console.WriteLine("Requested Roles:");
        foreach (var role in result.Roles)
        {
            System.Console.WriteLine($"  • {role.RoleTitle}");
        }

        WriteSeparator();
        System.Console.WriteLine("Recommended Team");
        WriteSeparator();

        var filled = 0;
        var partial = 0;
        var gaps = 0;

        foreach (var role in result.Roles)
        {
            if (role.Status == "FILLED")
            {
                filled++;
                WriteRoleFilled(role);
            }
            else if (role.Gap?.ReasonType == "ALLOCATED_ELSEWHERE")
            {
                partial++;
                WriteRolePartial(role);
            }
            else
            {
                gaps++;
                WriteRoleGap(role);
            }

            WriteSeparator('-');
        }

        WriteSummary(result, filled, partial, gaps);
        System.Console.WriteLine();
        System.Console.WriteLine(result.Disclaimer);
    }

    public static void WriteSkillMatchResults(SkillMatchResponseModel result, bool usedFallback)
    {
        WriteBanner("AI SKILL MATCH RESULTS");

        System.Console.WriteLine($"Project     : {result.ProjectName}");
        System.Console.WriteLine("Requirement :");
        WriteWrapped(result.Requirement);
        if (result.ParsedHoursPerWeek.HasValue)
        {
            System.Console.WriteLine($"Hours/week  : {result.ParsedHoursPerWeek}");
        }

        WriteSeparator();
        System.Console.WriteLine("Ranked Matches (organization-wide)");
        WriteSeparator();

        if (result.Matches.Count == 0)
        {
            System.Console.WriteLine("No matches returned.");
        }
        else
        {
            for (var i = 0; i < result.Matches.Count; i++)
            {
                var match = result.Matches[i];
                var skills = match.MatchedSkills is { Count: > 0 }
                    ? string.Join(", ", match.MatchedSkills)
                    : "—";

                System.Console.WriteLine();
                System.Console.WriteLine($"#{i + 1}  {match.EmployeeName}  (Profile ID {match.EmployeeId})");
                System.Console.WriteLine($"    Match Score   : {match.MatchScore}");
                System.Console.WriteLine($"    Matched Skills: {skills}");
                System.Console.WriteLine($"    Availability  : {match.AvailabilityPercent}%  ({match.UtilisationPercent}% allocated)");
                System.Console.WriteLine($"    Reason        : {match.Reason}");
            }
        }

        WriteSeparator();
        System.Console.WriteLine($"Candidates considered : {result.CandidatesConsidered}");
        System.Console.WriteLine();
        System.Console.WriteLine(result.Disclaimer);

        if (usedFallback)
        {
            System.Console.WriteLine("(Used offline ranking — configure LLM API key in Admin settings for live AI.)");
        }
    }

    private static void WriteRoleFilled(TeamBuilderRoleResultModel role)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"[✓] {role.RoleTitle}");
        System.Console.WriteLine();
        System.Console.WriteLine($"    Employee     : {role.AssignedEmployeeName}");
        System.Console.WriteLine($"    Skill Match  : {FormatPrimarySkill(role)}");
        System.Console.WriteLine($"    Match Score  : {role.MatchScore ?? 0}");
        System.Console.WriteLine($"    Availability : 100%");

        if (!string.IsNullOrWhiteSpace(role.Reason))
        {
            System.Console.WriteLine($"    Reason       : {role.Reason}");
        }
    }

    private static void WriteRolePartial(TeamBuilderRoleResultModel role)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"[⚠] {role.RoleTitle}");
        System.Console.WriteLine();
        System.Console.WriteLine("    No exact benched match found.");

        if (!string.IsNullOrWhiteSpace(role.Gap?.AlternativeEmployeeName))
        {
            System.Console.WriteLine($"    Closest Match : {role.Gap.AlternativeEmployeeName}");
        }

        if (!string.IsNullOrWhiteSpace(role.Gap?.Message))
        {
            System.Console.WriteLine($"    Note          : {role.Gap.Message}");
        }

        WriteBenchMatches(role);
    }

    private static void WriteRoleGap(TeamBuilderRoleResultModel role)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"[✗] {role.RoleTitle}");
        System.Console.WriteLine();

        if (role.BenchMatches.Count > 0)
        {
            var closest = role.BenchMatches[0];
            var skills = closest.MatchedSkills.Count == 0
                ? "—"
                : string.Join(", ", closest.MatchedSkills);
            System.Console.WriteLine("    No exact match assigned.");
            System.Console.WriteLine($"    Closest Benched : {closest.EmployeeName}");
            System.Console.WriteLine($"    Match Score     : {closest.MatchScore}");
            System.Console.WriteLine($"    Matched Skills  : {skills}");
        }
        else
        {
            System.Console.WriteLine("    No suitable candidate found.");
        }

        if (!string.IsNullOrWhiteSpace(role.Gap?.Message))
        {
            System.Console.WriteLine($"    Note            : {role.Gap.Message}");
        }

        if (role.BenchMatches.Count > 1)
        {
            WriteBenchMatches(role, startIndex: 1);
        }
    }

    private static void WriteBenchMatches(TeamBuilderRoleResultModel role, int startIndex = 0)
    {
        if (role.BenchMatches.Count <= startIndex)
        {
            return;
        }

        System.Console.WriteLine("    Benched alternatives:");
        for (var i = startIndex; i < role.BenchMatches.Count; i++)
        {
            var bench = role.BenchMatches[i];
            var skills = bench.MatchedSkills.Count == 0
                ? "—"
                : string.Join(", ", bench.MatchedSkills);
            System.Console.WriteLine(
                $"      {i + 1}. {bench.EmployeeName} — score {bench.MatchScore}, skills: {skills}");
        }
    }

    private static void WriteSummary(TeamBuilderResponseModel result, int filled, int partial, int gaps)
    {
        System.Console.WriteLine("Summary");
        WriteSeparator('-');
        System.Console.WriteLine($"Total Roles       : {result.Roles.Count}");
        System.Console.WriteLine($"Roles Filled      : {filled}");
        System.Console.WriteLine($"Partial Matches   : {partial}");
        System.Console.WriteLine($"Skill Gaps        : {gaps}");
        System.Console.WriteLine();
        System.Console.WriteLine($"Candidate Pool    : {result.CandidatesConsidered}");
        System.Console.WriteLine($"Fully Benched     : {result.AssignableCandidates}");
        System.Console.WriteLine(new string('=', BoxWidth));
    }

    private static string FormatPrimarySkill(TeamBuilderRoleResultModel role)
    {
        if (role.BenchMatches.Count > 0 && role.BenchMatches[0].MatchedSkills.Count > 0)
        {
            return role.BenchMatches[0].MatchedSkills[0];
        }

        if (role.RequiredSkills.Count > 0)
        {
            var skill = role.RequiredSkills[0];
            return skill.MinProficiency == "ANY"
                ? skill.SkillName
                : $"{skill.SkillName} ({skill.MinProficiency})";
        }

        return "—";
    }

    private static void WriteBanner(string title)
    {
        System.Console.WriteLine(new string('=', BoxWidth));
        System.Console.WriteLine(title.PadLeft((BoxWidth + title.Length) / 2));
        System.Console.WriteLine(new string('=', BoxWidth));
        System.Console.WriteLine();
    }

    private static void WriteSeparator(char character = '-')
    {
        System.Console.WriteLine(new string(character, BoxWidth));
    }

    private static void WriteWrapped(string text)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            System.Console.WriteLine($"  {line}");
        }
    }
}
