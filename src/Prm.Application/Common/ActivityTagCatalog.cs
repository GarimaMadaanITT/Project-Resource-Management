namespace Prm.Application.Common;

public static class ActivityTagCatalog
{
    public const string OtherTag = "Other";

    public static IReadOnlyList<string> PredefinedTags { get; } =
    [
        "Backend API Development",
        "Microservices / Architecture",
        "Database Design & Queries",
        "WebSocket / Real-time Features",
        "Frontend Development",
        "Code Review / Mentoring",
        "Bug Fixing",
        "DevOps / Deployment",
        "Testing & QA",
        "Documentation",
        OtherTag
    ];

    public static bool AllowsCustomOther => true;

    public static void ValidateTags(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            throw new Domain.Exceptions.DomainException("At least one activity tag is required.");
        }

        var hasOther = tags.Any(tag => string.Equals(tag, OtherTag, StringComparison.OrdinalIgnoreCase));
        var customTags = tags
            .Where(tag => !IsPredefinedTag(tag))
            .ToList();

        if (customTags.Count > 0 && !hasOther)
        {
            throw new Domain.Exceptions.DomainException(
                $"Custom activity tags require selecting '{OtherTag}' from the predefined list.");
        }

        foreach (var tag in tags.Where(IsPredefinedTag))
        {
            if (!PredefinedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                throw new Domain.Exceptions.DomainException($"Activity tag '{tag}' is not in the predefined catalog.");
            }
        }

        foreach (var customTag in customTags)
        {
            if (string.IsNullOrWhiteSpace(customTag))
            {
                throw new Domain.Exceptions.DomainException("Custom activity tags cannot be empty.");
            }
        }
    }

    public static string SerializeTags(IReadOnlyList<string> tags) =>
        string.Join(", ", tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0));

    public static string SerializeTagsForStorage(IReadOnlyList<string> tags) =>
        string.Join(", ", tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Where(tag => !string.Equals(tag, OtherTag, StringComparison.OrdinalIgnoreCase)));

    private static bool IsPredefinedTag(string tag) =>
        PredefinedTags.Any(predefined => string.Equals(predefined, tag, StringComparison.OrdinalIgnoreCase));
}
