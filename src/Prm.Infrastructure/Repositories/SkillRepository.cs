using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly PrmDbContext _context;

    public SkillRepository(PrmDbContext context)
    {
        _context = context;
    }

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Skills.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<EmployeeSkill?> GetEmployeeSkillAsync(int employeeId, int skillId, CancellationToken cancellationToken = default) =>
        _context.EmployeeSkills
            .Include(es => es.Skill)
            .FirstOrDefaultAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId, cancellationToken);

    public async Task AddEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
    {
        _context.EmployeeSkills.Add(employeeSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
    {
        _context.EmployeeSkills.Remove(employeeSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
