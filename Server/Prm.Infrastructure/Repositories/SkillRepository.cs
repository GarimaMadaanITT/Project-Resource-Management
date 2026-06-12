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

    public Task<UserSkill?> GetUserSkillAsync(int userId, int skillId, CancellationToken cancellationToken = default) =>
        _context.UserSkills
            .Include(us => us.Skill)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId, cancellationToken);

    public async Task AddUserSkillAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUserSkillAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        _context.UserSkills.Remove(userSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
