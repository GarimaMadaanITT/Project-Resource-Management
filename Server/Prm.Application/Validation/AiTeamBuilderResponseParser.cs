using System.Text.Json;
using System.Text.Json.Serialization;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class AiTeamBuilderResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<TeamBuilderRoleResultDto> Parse(string llmResponse)
    {
        try
        {
            var trimmed = ExtractJsonObject(llmResponse);
            var payload = JsonSerializer.Deserialize<ParsedPayload>(trimmed, JsonOptions);
            if (payload?.Roles is null || payload.Roles.Count == 0)
            {
                throw new DomainException("LLM response did not contain any team builder roles.");
            }

            return payload.Roles.Select(MapRole).ToList();
        }
        catch (JsonException exception)
        {
            throw new DomainException($"Invalid team builder LLM response: {exception.Message}");
        }
    }

    private static TeamBuilderRoleResultDto MapRole(ParsedRole role) =>
        new(
            role.RoleTitle?.Trim() ?? string.Empty,
            role.RequiredSkills?
                .Select(skill => new TeamBuilderSkillRequirementDto(
                    skill.SkillName?.Trim() ?? string.Empty,
                    NormalizeProficiency(skill.MinProficiency)))
                .ToList() ?? [],
            role.Status?.Trim().ToUpperInvariant() ?? string.Empty,
            string.IsNullOrWhiteSpace(role.AssignedEmployeeName) ? null : role.AssignedEmployeeName.Trim(),
            role.MatchScore,
            string.IsNullOrWhiteSpace(role.Reason) ? null : role.Reason.Trim(),
            role.Gap is null
                ? null
                : new TeamBuilderGapDto(
                    role.Gap.ReasonType?.Trim().ToUpperInvariant() ?? string.Empty,
                    role.Gap.Message?.Trim() ?? string.Empty,
                    string.IsNullOrWhiteSpace(role.Gap.AlternativeEmployeeName)
                        ? null
                        : role.Gap.AlternativeEmployeeName.Trim(),
                    string.IsNullOrWhiteSpace(role.Gap.AvailableFromDate)
                        ? null
                        : role.Gap.AvailableFromDate.Trim()),
            []);

    private static string NormalizeProficiency(string? proficiency)
    {
        if (string.IsNullOrWhiteSpace(proficiency))
        {
            return TeamBuilderConstants.ProficiencyAny;
        }

        var normalized = proficiency.Trim().ToUpperInvariant();
        return normalized == TeamBuilderConstants.ProficiencyAny
            ? TeamBuilderConstants.ProficiencyAny
            : normalized;
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

    private sealed record ParsedPayload(
        [property: JsonPropertyName("roles")] List<ParsedRole> Roles);

    private sealed record ParsedRole(
        [property: JsonPropertyName("roleTitle")] string? RoleTitle,
        [property: JsonPropertyName("requiredSkills")] List<ParsedSkill>? RequiredSkills,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("assignedEmployeeName")] string? AssignedEmployeeName,
        [property: JsonPropertyName("matchScore")] int? MatchScore,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("gap")] ParsedGap? Gap);

    private sealed record ParsedSkill(
        [property: JsonPropertyName("skillName")] string? SkillName,
        [property: JsonPropertyName("minProficiency")] string? MinProficiency);

    private sealed record ParsedGap(
        [property: JsonPropertyName("reasonType")] string? ReasonType,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("alternativeEmployeeName")] string? AlternativeEmployeeName,
        [property: JsonPropertyName("availableFromDate")] string? AvailableFromDate);
}
