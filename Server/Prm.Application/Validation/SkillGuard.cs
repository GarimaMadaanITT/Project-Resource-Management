using Prm.Domain.Entities;
using Prm.Domain.Exceptions;
using Prm.Application.Common;

namespace Prm.Application.Validation;

public static class SkillGuard
{
    public static void EnsureNotDuplicate(UserSkill? existingSkill)
    {
        if (existingSkill is not null)
        {
            throw new ConflictException(ErrorMessages.EmployeeSkillDuplicate);
        }
    }
}
