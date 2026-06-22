using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Employees;

public class AdminEmployeeSkillService
{
    private readonly IEmployeeRepository _employees;
    private readonly ISkillRepository _skills;
    private readonly IAuditLogService _auditLog;

    public AdminEmployeeSkillService(
        IEmployeeRepository employees,
        ISkillRepository skills,
        IAuditLogService auditLog)
    {
        _employees = employees;
        _skills = skills;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        return employee.Skills.Select(MapSkill).ToList();
    }

    public async Task<EmployeeSkillDto> AddSkillAsync(
        int id,
        AddEmployeeSkillRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        EmployeeGuard.EnsureActive(employee);

        var skillName = StringGuard.RequireNonEmpty(request.SkillName, "Skill name");
        var category = EnumGuard.Parse<SkillCategory>(
            StringGuard.RequireNonEmpty(request.Category, "Category"),
            "Category");
        var proficiency = EnumGuard.Parse<ProficiencyLevel>(
            StringGuard.RequireNonEmpty(request.Proficiency, "Proficiency"),
            "Proficiency");

        var skill = await _skills.GetByNameAsync(skillName, cancellationToken);
        if (skill is null)
        {
            skill = new Skill { Name = skillName, Category = category };
            await _skills.AddAsync(skill, cancellationToken);
        }

        var existing = await _skills.GetEmployeeSkillAsync(id, skill.Id, cancellationToken);
        SkillGuard.EnsureNotDuplicate(existing);

        var employeeSkill = new EmployeeSkill
        {
            EmployeeId = id,
            SkillId = skill.Id,
            Proficiency = proficiency
        };
        await _skills.AddEmployeeSkillAsync(employeeSkill, cancellationToken);
        await _skills.SaveChangesAsync(cancellationToken);

        employeeSkill.Skill = skill;

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.EmployeeSkill,
            id,
            AuditConstants.Actions.Created,
            null,
            AuditSnapshotBuilder.EmployeeSkillSnapshot(employeeSkill),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        return MapSkill(employeeSkill);
    }

    public async Task<EmployeeSkillDto> UpdateSkillAsync(
        int id,
        int skillId,
        UpdateEmployeeSkillRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        EmployeeGuard.EnsureActive(employee);

        var employeeSkill = EntityGuard.EnsureFound(
            await _skills.GetEmployeeSkillAsync(id, skillId, cancellationToken),
            ErrorMessages.EmployeeSkillNotFound);

        var oldSnapshot = AuditSnapshotBuilder.EmployeeSkillSnapshot(employeeSkill);

        employeeSkill.Proficiency = EnumGuard.Parse<ProficiencyLevel>(
            StringGuard.RequireNonEmpty(request.Proficiency, "Proficiency"),
            "Proficiency");
        await _skills.SaveChangesAsync(cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.EmployeeSkill,
            id,
            AuditConstants.Actions.Updated,
            oldSnapshot,
            AuditSnapshotBuilder.EmployeeSkillSnapshot(employeeSkill),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        return MapSkill(employeeSkill);
    }

    public async Task RemoveSkillAsync(
        int id,
        int skillId,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        EmployeeGuard.EnsureActive(employee);

        var employeeSkill = EntityGuard.EnsureFound(
            await _skills.GetEmployeeSkillAsync(id, skillId, cancellationToken),
            ErrorMessages.EmployeeSkillNotFound);

        var oldSnapshot = AuditSnapshotBuilder.EmployeeSkillSnapshot(employeeSkill);

        await _skills.RemoveEmployeeSkillAsync(employeeSkill, cancellationToken);
        await _skills.SaveChangesAsync(cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.EmployeeSkill,
            id,
            AuditConstants.Actions.Removed,
            oldSnapshot,
            null,
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);
    }

    private static EmployeeSkillDto MapSkill(EmployeeSkill employeeSkill) =>
        new(
            employeeSkill.SkillId,
            employeeSkill.Skill.Name,
            employeeSkill.Skill.Category.ToString(),
            employeeSkill.Proficiency.ToString());
}
