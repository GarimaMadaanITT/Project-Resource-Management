using System.Text.Json;
using System.Text.Json.Serialization;
using Prm.Application.DTOs.Manager;

namespace Prm.Application.Validation;

public static class AiMatchResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<SkillMatchResultItem> Parse(
        string llmResponse,
        IReadOnlyDictionary<int, AiCapacityFilter.CandidateSnapshot> candidatesById)
    {
        var payload = TryDeserialize(llmResponse);
        if (payload is null || payload.Matches.Count == 0)
        {
            return BuildFallbackMatches(candidatesById.Values);
        }

        var results = new List<SkillMatchResultItem>();
        foreach (var match in payload.Matches)
        {
            if (!candidatesById.TryGetValue(match.EmployeeId, out var candidate))
            {
                continue;
            }

            results.Add(new SkillMatchResultItem(
                candidate.ResourceProfile.Id,
                candidate.ResourceProfile.User.FullName,
                candidate.UtilisationPercent,
                candidate.AvailabilityPercent,
                string.IsNullOrWhiteSpace(match.Reason)
                    ? "Suggested based on skills and availability."
                    : match.Reason.Trim()));
        }

        return results.Count == 0
            ? BuildFallbackMatches(candidatesById.Values)
            : results;
    }

    public static string BuildSkillMatchPrompt(
        string projectName,
        string requirement,
        IReadOnlyList<AiCapacityFilter.CandidateSnapshot> candidates)
    {
        var lines = candidates.Select(candidate =>
            $"- ID {candidate.ResourceProfile.Id}: {candidate.ResourceProfile.User.FullName}; " +
            $"dept {candidate.ResourceProfile.User.Department}; " +
            $"designation {candidate.ResourceProfile.User.Designation}; " +
            $"util {candidate.UtilisationPercent}%; free {candidate.FreeHoursPerWeek} hrs/week; " +
            $"skills: {string.Join(", ", candidate.ProfileSkills)}; " +
            $"recent tags: {string.Join(", ", candidate.RecentActivityTags)}");

        return
            $"Project: {projectName}\n" +
            $"Requirement: {requirement}\n\n" +
            "Qualified organization candidates (pre-filtered by capacity):\n" +
            string.Join("\n", lines);
    }

    public static string BuildRiskSummaryPrompt(
        string projectName,
        string healthStatus,
        DateOnly endDate,
        IReadOnlyList<string> milestoneFacts,
        IReadOnlyList<string> allocationFacts,
        IReadOnlyList<string> timesheetFacts,
        IReadOnlyList<string> riskFlags)
    {
        return
            $"Project: {projectName}\n" +
            $"Health status: {healthStatus}\n" +
            $"End date: {endDate:yyyy-MM-dd}\n\n" +
            "Milestones:\n" + string.Join("\n", milestoneFacts.Select(f => $"- {f}")) + "\n\n" +
            "Allocations:\n" + string.Join("\n", allocationFacts.Select(f => $"- {f}")) + "\n\n" +
            "Recent timesheet facts:\n" + string.Join("\n", timesheetFacts.Select(f => $"- {f}")) + "\n\n" +
            "Risk flags:\n" + string.Join("\n", riskFlags.Select(f => $"- {f}"));
    }

    private static ParsedPayload? TryDeserialize(string llmResponse)
    {
        try
        {
            var trimmed = ExtractJsonObject(llmResponse);
            return JsonSerializer.Deserialize<ParsedPayload>(trimmed, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return text;
    }

    private static IReadOnlyList<SkillMatchResultItem> BuildFallbackMatches(
        IEnumerable<AiCapacityFilter.CandidateSnapshot> candidates) =>
        candidates
            .Take(3)
            .Select(candidate => new SkillMatchResultItem(
                candidate.ResourceProfile.Id,
                candidate.ResourceProfile.User.FullName,
                candidate.UtilisationPercent,
                candidate.AvailabilityPercent,
                "Ranked by profile skills and available capacity."))
            .ToList();

    private sealed record ParsedPayload(
        [property: JsonPropertyName("matches")] List<ParsedMatch> Matches);

    private sealed record ParsedMatch(
        [property: JsonPropertyName("employeeId")] int EmployeeId,
        [property: JsonPropertyName("reason")] string Reason);
}
