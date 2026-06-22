using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class AiSkillMatcher
{
    private static readonly Dictionary<string, string[]> SkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sdet"] = ["selenium", "cypress", "manual testing", "automation", "testing", "qa"],
        ["sedts"] = ["selenium", "cypress", "manual testing", "automation", "testing", "qa", "sdet"],
        ["sedt"] = ["selenium", "cypress", "manual testing", "automation", "testing", "qa", "sdet"],
        ["qa"] = ["selenium", "manual testing", "testing", "cypress", "sdet"],
        ["tester"] = ["selenium", "manual testing", "testing", "cypress", "sdet"],
        ["automation"] = ["selenium", "cypress", "testing"],
        ["python"] = ["numpy", "pandas", "django", "flask", "fastapi", "pytest"],
        ["java"] = ["spring boot", "spring", "kotlin", "maven", "hibernate"],
        ["javascript"] = ["typescript", "react", "node", "nodejs", "vue", "angular"],
        ["frontend"] = ["react", "typescript", "css", "html", "javascript", "vue", "angular"],
        ["backend"] = ["java", "spring boot", "python", "django", "node", "mysql", "postgresql"],
        ["devops"] = ["docker", "kubernetes", "ci/cd", "terraform", "aws", "jenkins"],
        ["full stack"] = ["react", "java", "spring boot", "node", "typescript"],
        ["microservices"] = ["java", "spring boot", "docker", "kubernetes", "api"],
    };

    private static readonly Dictionary<string, string[]> DepartmentRoleHints = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Department.QA)] = ["sdet", "selenium", "testing", "qa", "manual testing", "cypress"],
        [nameof(Department.DevOps)] = ["docker", "kubernetes", "ci/cd", "devops", "terraform"],
        [nameof(Department.Backend)] = ["java", "spring boot", "python", "django", "mysql", "api"],
        [nameof(Department.Frontend)] = ["react", "typescript", "css", "javascript", "ui"],
        [nameof(Department.Engineering)] = ["java", "python", "react", "spring boot"],
    };

    private static readonly Dictionary<string, string[]> DesignationRoleHints = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Designation.QAEngineer)] = ["sdet", "selenium", "testing", "qa"],
        [nameof(Designation.DevOpsEngineer)] = ["docker", "kubernetes", "devops", "ci/cd"],
        [nameof(Designation.SoftwareEngineer)] = ["java", "python", "react", "spring boot"],
        [nameof(Designation.SeniorSoftwareEngineer)] = ["java", "python", "react", "spring boot", "microservices"],
    };

    public static IReadOnlyList<string> ExpandKeywords(IEnumerable<string> keywords)
    {
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            var trimmed = keyword.Trim();
            expanded.Add(trimmed);

            if (SkillAliases.TryGetValue(trimmed, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    expanded.Add(alias);
                }
            }
        }

        return expanded.ToList();
    }

    public static IReadOnlyList<string> ExpandRequirement(string requirement)
    {
        var tokens = Tokenize(requirement);
        return ExpandKeywords(tokens);
    }

    public static bool SkillMatchesKeyword(string skillName, string keyword)
    {
        if (string.IsNullOrWhiteSpace(skillName) || string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        if (skillName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || keyword.Contains(skillName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (SkillAliases.TryGetValue(keyword, out var aliases))
        {
            return aliases.Any(alias =>
                skillName.Contains(alias, StringComparison.OrdinalIgnoreCase)
                || alias.Contains(skillName, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var (role, roleAliases) in SkillAliases)
        {
            if (!keyword.Contains(role, StringComparison.OrdinalIgnoreCase)
                && !role.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (roleAliases.Any(alias =>
                    skillName.Contains(alias, StringComparison.OrdinalIgnoreCase)
                    || alias.Contains(skillName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<string> GetMatchedSkillLabels(
        IEnumerable<string> skillNames,
        IReadOnlyList<string> expandedKeywords)
    {
        return skillNames
            .Where(skill => expandedKeywords.Any(keyword => SkillMatchesKeyword(skill, keyword)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int ScoreCandidateSkills(
        IEnumerable<string> skillNames,
        IReadOnlyList<string> expandedKeywords,
        string? department,
        string? designation)
    {
        var skills = skillNames.ToList();
        var matchedSkills = GetMatchedSkillLabels(skills, expandedKeywords);
        var score = matchedSkills.Count * 30;

        if (expandedKeywords.Any(keyword =>
                !string.IsNullOrWhiteSpace(department)
                && DepartmentRoleHints.TryGetValue(department, out var hints)
                && hints.Any(hint => hint.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || keyword.Contains(hint, StringComparison.OrdinalIgnoreCase))))
        {
            score += 20;
        }

        if (expandedKeywords.Any(keyword =>
                !string.IsNullOrWhiteSpace(designation)
                && designation.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            score += 15;
        }

        if (!string.IsNullOrWhiteSpace(designation)
            && DesignationRoleHints.TryGetValue(designation, out var designationHints)
            && expandedKeywords.Any(keyword =>
                designationHints.Any(hint =>
                    hint.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || keyword.Contains(hint, StringComparison.OrdinalIgnoreCase))))
        {
            score += 15;
        }

        if (expandedKeywords.Any(keyword =>
                keyword.Contains("developer", StringComparison.OrdinalIgnoreCase)
                && designation?.Contains("Engineer", StringComparison.OrdinalIgnoreCase) == true))
        {
            score += 10;
        }

        return score;
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split([' ', ',', '.', ';', ':', '-', '/', '(', ')', '•', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.Trim())
            .Where(word => word.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
