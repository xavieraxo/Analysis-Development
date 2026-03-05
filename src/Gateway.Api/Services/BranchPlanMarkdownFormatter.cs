using System.Text;
using Gateway.Api.DTOs;

namespace Gateway.Api.Services;

/// <summary>
/// Formatea un BranchPlanExportDto como Markdown.
/// </summary>
public static class BranchPlanMarkdownFormatter
{
    /// <summary>
    /// Genera Markdown determinista para el Branch Plan.
    /// </summary>
    public static string ToMarkdown(BranchPlanExportDto dto, int runId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Branch Plan – DevFlow Run {runId}");
        sb.AppendLine();
        sb.AppendLine("| Order | Story | Task | Branch | Title |");
        sb.AppendLine("|------:|------:|------|--------|-------|");

        foreach (var item in dto.Items.OrderBy(i => i.Order))
        {
            var titleEscaped = item.Title.Replace("|", "\\|").Replace("\n", " ");
            var branchEscaped = item.BranchName.Replace("|", "\\|");
            sb.AppendLine($"| {item.Order} | {item.StoryId} | {item.TaskId} | {branchEscaped} | {titleEscaped} |");
        }

        var withDescription = dto.Items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();
        if (withDescription.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Descripciones");
            sb.AppendLine();
            foreach (var item in withDescription.OrderBy(i => i.Order))
            {
                sb.AppendLine($"### {item.TaskId} - {item.Title}");
                sb.AppendLine();
                sb.AppendLine(item.Description);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
