using Prm.Application.Common;

using Prm.Application.DTOs.Admin;

using Prm.Application.Interfaces;

using Prm.Application.Validation;

using Prm.Domain.Entities;

using Prm.Domain.Enums;



namespace Prm.Application.Services.Admin.Employees;



public class AdminEmployeeSkillService

{

    private readonly IResourceProfileRepository _resourceProfiles;

    private readonly ISkillRepository _skills;

    private readonly IAuditLogService _auditLog;



    public AdminEmployeeSkillService(

        IResourceProfileRepository resourceProfiles,

        ISkillRepository skills,

        IAuditLogService auditLog)

    {

        _resourceProfiles = resourceProfiles;

        _skills = skills;

        _auditLog = auditLog;

    }



    public async Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(int id, CancellationToken cancellationToken = default)

    {

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        return resourceProfile.User.Skills.Select(MapSkill).ToList();

    }



    public async Task<EmployeeSkillDto> AddSkillAsync(

        int id,

        AddEmployeeSkillRequest request,

        int actingUserId,

        CancellationToken cancellationToken = default)

    {

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        EmployeeGuard.EnsureActive(resourceProfile);



        var skillName = StringGuard.RequireNonEmpty(request.SkillName, "Skill name");

        var (category, customCategoryLabel) = ResolveCategory(request.Category, request.CustomCategory);

        var proficiency = EnumGuard.Parse<ProficiencyLevel>(

            StringGuard.RequireNonEmpty(request.Proficiency, "Proficiency"),

            "Proficiency");



        var skill = await _skills.GetByNameAsync(skillName, cancellationToken);

        if (skill is null)

        {

            skill = new Skill

            {

                Name = skillName,

                Category = category,

                CustomCategoryLabel = customCategoryLabel

            };

            await _skills.AddAsync(skill, cancellationToken);

        }



        var existing = await _skills.GetUserSkillAsync(resourceProfile.UserId, skill.Id, cancellationToken);

        SkillGuard.EnsureNotDuplicate(existing);



        var userSkill = new UserSkill

        {

            UserId = resourceProfile.UserId,

            SkillId = skill.Id,

            Proficiency = proficiency

        };

        await _skills.AddUserSkillAsync(userSkill, cancellationToken);

        await _skills.SaveChangesAsync(cancellationToken);



        userSkill.Skill = skill;



        await _auditLog.AuditAsync(

            AuditConstants.EntityNames.UserSkill,

            resourceProfile.UserId,

            AuditConstants.Actions.Created,

            null,

            AuditSnapshotBuilder.UserSkillSnapshot(userSkill),

            actingUserId,

            AuthConstants.RoleName(UserRole.Admin),

            AuditConstants.Sources.User,

            cancellationToken);



        return MapSkill(userSkill);

    }



    public async Task<EmployeeSkillDto> UpdateSkillAsync(

        int id,

        int skillId,

        UpdateEmployeeSkillRequest request,

        int actingUserId,

        CancellationToken cancellationToken = default)

    {

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        EmployeeGuard.EnsureActive(resourceProfile);



        var userSkill = EntityGuard.EnsureFound(

            await _skills.GetUserSkillAsync(resourceProfile.UserId, skillId, cancellationToken),

            ErrorMessages.EmployeeSkillNotFound);



        var oldSnapshot = AuditSnapshotBuilder.UserSkillSnapshot(userSkill);



        userSkill.Proficiency = EnumGuard.Parse<ProficiencyLevel>(

            StringGuard.RequireNonEmpty(request.Proficiency, "Proficiency"),

            "Proficiency");

        await _skills.SaveChangesAsync(cancellationToken);



        await _auditLog.AuditAsync(

            AuditConstants.EntityNames.UserSkill,

            resourceProfile.UserId,

            AuditConstants.Actions.Updated,

            oldSnapshot,

            AuditSnapshotBuilder.UserSkillSnapshot(userSkill),

            actingUserId,

            AuthConstants.RoleName(UserRole.Admin),

            AuditConstants.Sources.User,

            cancellationToken);



        return MapSkill(userSkill);

    }



    public async Task RemoveSkillAsync(

        int id,

        int skillId,

        int actingUserId,

        CancellationToken cancellationToken = default)

    {

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        EmployeeGuard.EnsureActive(resourceProfile);



        var userSkill = EntityGuard.EnsureFound(

            await _skills.GetUserSkillAsync(resourceProfile.UserId, skillId, cancellationToken),

            ErrorMessages.EmployeeSkillNotFound);



        var oldSnapshot = AuditSnapshotBuilder.UserSkillSnapshot(userSkill);



        await _skills.RemoveUserSkillAsync(userSkill, cancellationToken);

        await _skills.SaveChangesAsync(cancellationToken);



        await _auditLog.AuditAsync(

            AuditConstants.EntityNames.UserSkill,

            resourceProfile.UserId,

            AuditConstants.Actions.Removed,

            oldSnapshot,

            null,

            actingUserId,

            AuthConstants.RoleName(UserRole.Admin),

            AuditConstants.Sources.User,

            cancellationToken);

    }



    private static EmployeeSkillDto MapSkill(UserSkill userSkill) =>

        new(

            userSkill.SkillId,

            userSkill.Skill.Name,

            userSkill.Skill.CustomCategoryLabel ?? userSkill.Skill.Category.ToString(),

            userSkill.Proficiency.ToString());



    private static (SkillCategory Category, string? CustomCategoryLabel) ResolveCategory(

        string category,

        string? customCategory)

    {

        var normalized = StringGuard.RequireNonEmpty(category, "Category");

        if (normalized.Equals(nameof(SkillCategory.Other), StringComparison.OrdinalIgnoreCase))

        {

            var label = StringGuard.RequireNonEmpty(customCategory, "Custom category");

            return (SkillCategory.Other, label.Trim());

        }



        return (EnumGuard.Parse<SkillCategory>(normalized, "Category"), null);

    }

}


