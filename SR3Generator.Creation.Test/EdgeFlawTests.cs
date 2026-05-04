using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;

namespace SR3Generator.Creation.Test;

public class EdgeFlawTests
{
    private static SkillDatabase CreateSkillDatabase()
    {
        var options = Options.Create(new DatabaseOptions());
        var dbConnectionFactory = new DbConnectionFactory(options);
        var queryHandler = new ReadSkillsQueryHandler();
        return new SkillDatabase(dbConnectionFactory, queryHandler);
    }

    private static CharacterBuilder CreateBuilder()
    {
        return new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
    }

    [Fact]
    public void AddEdgeFlaw_IncreasesCount()
    {
        var builder = CreateBuilder();
        var edge = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Perceptive");

        builder.AddEdgeFlaw(edge);

        Assert.Single(builder.Character.EdgesFlaws);
        Assert.Equal(3, builder.EdgePoints);
        Assert.Equal(0, builder.FlawPoints);
        Assert.Equal(3, builder.NetEdgeFlawPoints);
    }

    [Fact]
    public void AddFlaw_IncreasesFlawPoints()
    {
        var builder = CreateBuilder();
        var flaw = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Color Blind");

        builder.AddEdgeFlaw(flaw);

        Assert.Single(builder.Character.EdgesFlaws);
        Assert.Equal(0, builder.EdgePoints);
        Assert.Equal(1, builder.FlawPoints);
        Assert.Equal(-1, builder.NetEdgeFlawPoints);
    }

    [Fact]
    public void RemoveEdgeFlaw_DecreasesCount()
    {
        var builder = CreateBuilder();
        var edge = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Perceptive");
        builder.AddEdgeFlaw(edge);
        var id = builder.Character.EdgesFlaws[0].Id;

        builder.RemoveEdgeFlaw(id);

        Assert.Empty(builder.Character.EdgesFlaws);
        Assert.Equal(0, builder.EdgePoints);
    }

    [Fact]
    public void Validation_BalancedEdgesFlaws_IsValid()
    {
        var builder = CreateBuilder();
        var edge = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Perceptive"); // +3
        var flaw = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Color Blind"); // -1
        var flaw2 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Oblivious"); // -2

        builder.AddEdgeFlaw(edge);
        builder.AddEdgeFlaw(flaw);
        builder.AddEdgeFlaw(flaw2);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Empty(edgeFlawIssues);
    }

    [Fact]
    public void Validation_UnbalancedEdgesFlaws_IsError()
    {
        var builder = CreateBuilder();
        var edge = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Perceptive"); // +3

        builder.AddEdgeFlaw(edge);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("balance to 0 net points"));
    }

    [Fact]
    public void Validation_TooManyEdgePoints_IsError()
    {
        var builder = CreateBuilder();
        // Add 7 points of edges and matching flaws
        var edge1 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Photographic Memory"); // +3
        var edge2 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Perceptive"); // +3
        var edge3 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Bravery"); // +1
        var flaw = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Cursed Karma"); // -6
        var flaw2 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Bad Karma"); // -5

        builder.AddEdgeFlaw(edge1);
        builder.AddEdgeFlaw(edge2);
        builder.AddEdgeFlaw(edge3);
        builder.AddEdgeFlaw(flaw);
        builder.AddEdgeFlaw(flaw2);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("Edges exceed maximum of 6 points"));
    }

    [Fact]
    public void Validation_TooManyFlawPoints_IsError()
    {
        var builder = CreateBuilder();
        var edge = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Photographic Memory"); // +3
        var flaw1 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Cursed Karma"); // -6
        var flaw2 = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Bad Karma"); // -5

        builder.AddEdgeFlaw(edge);
        builder.AddEdgeFlaw(flaw1);
        builder.AddEdgeFlaw(flaw2);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("Flaws exceed maximum of 6 points"));
    }

    [Fact]
    public void Validation_MutuallyExclusive_PacifistAndTotalPacifist_IsError()
    {
        var builder = CreateBuilder();
        var pacifist = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Pacifist");
        var totalPacifist = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Total Pacifist");

        builder.AddEdgeFlaw(pacifist);
        builder.AddEdgeFlaw(totalPacifist);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("Cannot take both Pacifist and Total Pacifist"));
    }

    [Fact]
    public void Validation_MutuallyExclusive_BlindAndColorBlind_IsError()
    {
        var builder = CreateBuilder();
        var blind = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Blind");
        var colorBlind = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Color Blind");

        builder.AddEdgeFlaw(blind);
        builder.AddEdgeFlaw(colorBlind);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("Cannot take both Blind and Color Blind"));
    }

    [Fact]
    public void Validation_MutuallyExclusive_BioRejectionAndSensitiveSystem_IsError()
    {
        var builder = CreateBuilder();
        var bioRejection = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Bio-Rejection");
        var sensitiveSystem = EdgeFlawDatabase.AllEdgesFlaws.First(ef => ef.Name == "Sensitive System");

        builder.AddEdgeFlaw(bioRejection);
        builder.AddEdgeFlaw(sensitiveSystem);

        builder.Validate();
        var edgeFlawIssues = builder.ValidationIssues.Where(i => i.Category == Creation.Validation.ValidationIssueCategory.EdgesFlaws).ToList();
        Assert.Contains(edgeFlawIssues, i => i.Message.Contains("Cannot take both Bio-Rejection and Sensitive System"));
    }

    [Fact]
    public void EdgeFlawDatabase_ContainsEdgesAndFlaws()
    {
        var all = EdgeFlawDatabase.AllEdgesFlaws;
        Assert.NotEmpty(all);
        Assert.Contains(all, ef => ef.Type == EdgeFlawType.Edge);
        Assert.Contains(all, ef => ef.Type == EdgeFlawType.Flaw);
    }

    [Fact]
    public void EdgeFlawDatabase_HasUniqueNames()
    {
        var all = EdgeFlawDatabase.AllEdgesFlaws;
        var names = all.Select(ef => ef.Name).ToList();
        var distinct = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(distinct.Count, names.Count);
    }
}
