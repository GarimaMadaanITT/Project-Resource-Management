using Prm.Application.Common;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class TeamBuilderRequirementValidator
{
    public static string Validate(string? requirement)
    {
        var trimmed = StringGuard.RequireNonEmpty(requirement, "Requirement");

        if (trimmed.Length > ValidationConstants.MaxTeamBuilderRequirementLength)
        {
            throw new DomainException(
                $"Requirement cannot exceed {ValidationConstants.MaxTeamBuilderRequirementLength} characters.");
        }

        return trimmed;
    }
}
