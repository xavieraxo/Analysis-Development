using System.Text;
using System.Text.RegularExpressions;

namespace Gateway.Api.Services;

/// <summary>
/// Implementación por defecto del generador de nombres de rama.
/// Formato: {prefix}/{area}/{taskId}-{slug}
/// </summary>
public sealed class DefaultBranchNameGenerator : IBranchNameGenerator
{
    private const int MaxSlugLength = 60;
    private static readonly Regex NonAlphanumericOrHyphen = new(@"[^a-z0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphens = new(@"-+", RegexOptions.Compiled);

    /// <inheritdoc />
    public string Generate(BranchNameInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Area))
            throw new ArgumentException("Area no puede estar vacío.", nameof(input));

        var prefix = input.Prefix switch
        {
            BranchPrefix.Feature => "feature",
            BranchPrefix.Fix => "fix",
            BranchPrefix.Chore => "chore",
            _ => "feature"
        };

        var area = ToSlug(input.Area.Trim());
        var taskId = SanitizeTaskId(input.TaskId?.Trim() ?? string.Empty);
        var slug = BuildSlug(input.Title?.Trim() ?? string.Empty, taskId);

        var result = string.IsNullOrEmpty(slug)
            ? $"{prefix}/{area}/{taskId}"
            : $"{prefix}/{area}/{taskId}-{slug}";

        return result;
    }

    private static string ToSlug(string value)
    {
        var normalized = value.ToLowerInvariant();
        normalized = NonAlphanumericOrHyphen.Replace(normalized, "-");
        normalized = MultipleHyphens.Replace(normalized, "-");
        return normalized.Trim('-');
    }

    private static string SanitizeTaskId(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
            return "task";
        var sb = new StringBuilder();
        foreach (var c in taskId.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '.' || c == '-')
                sb.Append(c);
        }
        var result = sb.ToString().Trim('.', '-');
        return string.IsNullOrEmpty(result) ? "task" : result;
    }

    private static string BuildSlug(string title, string taskId)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slug = ToSlug(title);
        if (slug.Length > MaxSlugLength)
            slug = slug[..MaxSlugLength].TrimEnd('-');
        return slug;
    }
}
