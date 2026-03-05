using Gateway.Api.Services;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests unitarios para DefaultBranchNameGenerator.
/// </summary>
public class BranchNameGeneratorTests
{
    private readonly IBranchNameGenerator _generator = new DefaultBranchNameGenerator();

    [Fact]
    public void Generate_FormatoCorrecto_PrefixAreaSlug()
    {
        var input = new BranchNameInput
        {
            Area = "autodev",
            StoryId = "5.2",
            TaskId = "5.2.1",
            Title = "Create run endpoint",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.Equal("feature/autodev/5.2.1-create-run-endpoint", result);
    }

    [Fact]
    public void Generate_SanitizaCaracteresEspeciales()
    {
        var input = new BranchNameInput
        {
            Area = "rag",
            TaskId = "5.2.1",
            Title = "Implementar IRetriever (PostgreSQL + pgvector)",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.Equal("feature/rag/5.2.1-implementar-iretriever-postgresql-pgvector", result);
    }

    [Fact]
    public void Generate_ColapsaEspacios()
    {
        var input = new BranchNameInput
        {
            Area = "autodev",
            TaskId = "5.2.1",
            Title = "  Add   execute   stage   ",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.Equal("feature/autodev/5.2.1-add-execute-stage", result);
    }

    [Fact]
    public void Generate_RecortaLongitud()
    {
        var input = new BranchNameInput
        {
            Area = "guardrails",
            TaskId = "6.1",
            Title = "Implementar validación de políticas de seguridad y límites de tokens para evitar costos excesivos en llamadas LLM",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.StartsWith("feature/guardrails/6.1-", result);
        var slugPart = result["feature/guardrails/6.1-".Length..];
        Assert.True(slugPart.Length <= 60, "El slug debe recortarse a max 60 caracteres");
    }

    [Fact]
    public void Generate_TitleVacio_UsaSoloTaskId()
    {
        var input = new BranchNameInput
        {
            Area = "autodev",
            StoryId = "5.2",
            TaskId = "5.2.1",
            Title = "",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.Equal("feature/autodev/5.2.1", result);
    }

    [Fact]
    public void Generate_PrefixFix_GeneraFix()
    {
        var input = new BranchNameInput
        {
            Area = "auth",
            TaskId = "1.1",
            Title = "Fix login timeout",
            Prefix = BranchPrefix.Fix
        };

        var result = _generator.Generate(input);

        Assert.Equal("fix/auth/1.1-fix-login-timeout", result);
    }

    [Fact]
    public void Generate_PrefixChore_GeneraChore()
    {
        var input = new BranchNameInput
        {
            Area = "deps",
            TaskId = "0.1",
            Title = "Update packages",
            Prefix = BranchPrefix.Chore
        };

        var result = _generator.Generate(input);

        Assert.Equal("chore/deps/0.1-update-packages", result);
    }

    [Fact]
    public void Generate_AreaConMayusculas_NormalizaALowercase()
    {
        var input = new BranchNameInput
        {
            Area = "AutoDev",
            TaskId = "5.2.1",
            Title = "Test",
            Prefix = BranchPrefix.Feature
        };

        var result = _generator.Generate(input);

        Assert.Equal("feature/autodev/5.2.1-test", result);
    }

    [Fact]
    public void Generate_AreaVacio_LanzaArgumentException()
    {
        var input = new BranchNameInput
        {
            Area = "",
            TaskId = "5.2.1",
            Title = "Test",
            Prefix = BranchPrefix.Feature
        };

        var ex = Assert.Throws<ArgumentException>(() => _generator.Generate(input));

        Assert.Contains("Area", ex.Message);
    }

    [Fact]
    public void Generate_AreaSoloEspacios_LanzaArgumentException()
    {
        var input = new BranchNameInput
        {
            Area = "   ",
            TaskId = "5.2.1",
            Title = "Test",
            Prefix = BranchPrefix.Feature
        };

        Assert.Throws<ArgumentException>(() => _generator.Generate(input));
    }
}
