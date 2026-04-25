using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;

namespace SR3Generator.Database.Test;

public class MatrixQueryTests
{
    private static CyberdeckDatabase MakeCyberdeckDatabase()
    {
        var options = Options.Create(new DatabaseOptions());
        return new CyberdeckDatabase(options);
    }

    private static ProgramDatabase MakeProgramDatabase()
    {
        var options = Options.Create(new DatabaseOptions());
        return new ProgramDatabase(options);
    }

    [Fact]
    public void CyberdeckDatabase_LoadsCatalog()
    {
        var db = MakeCyberdeckDatabase();
        Assert.NotEmpty(db.AllCyberdecks);
    }

    [Fact]
    public void CyberdeckDatabase_PersonaColumnMapsToMPCPAndPersonaFields()
    {
        var db = MakeCyberdeckDatabase();
        // Allegiance Sigma ships with Persona (MPCP) > 0 in the DB. BEMS defaults must satisfy
        // the SR3 rules: each stat ≤ MPCP and sum ≤ 3×MPCP (validated by CyberdeckValidator).
        var sigma = db.AllCyberdecks.FirstOrDefault(d =>
            d.Name.Contains("Allegiance Sigma", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(sigma);
        Assert.True(sigma!.MPCP > 0);
        Assert.True(sigma.Bod <= sigma.MPCP);
        Assert.True(sigma.Evasion <= sigma.MPCP);
        Assert.True(sigma.Masking <= sigma.MPCP);
        Assert.True(sigma.Sensor <= sigma.MPCP);
        Assert.True(sigma.Bod + sigma.Evasion + sigma.Masking + sigma.Sensor <= sigma.MPCP * 3);
    }

    [Fact]
    public void CyberdeckDatabase_AllDecksPassBemsConstraints()
    {
        var db = MakeCyberdeckDatabase();
        // Every deck from the catalog should satisfy the SR3 persona rules out of the box,
        // otherwise the validator flags every newly-bought stock deck as broken.
        Assert.All(db.AllCyberdecks.Where(d => d.MPCP > 0), d =>
        {
            Assert.True(d.Bod <= d.MPCP, $"{d.Name}: Bod {d.Bod} > MPCP {d.MPCP}");
            Assert.True(d.Evasion <= d.MPCP, $"{d.Name}: Evasion {d.Evasion} > MPCP {d.MPCP}");
            Assert.True(d.Masking <= d.MPCP, $"{d.Name}: Masking {d.Masking} > MPCP {d.MPCP}");
            Assert.True(d.Sensor <= d.MPCP, $"{d.Name}: Sensor {d.Sensor} > MPCP {d.MPCP}");
            Assert.True(d.Bod + d.Evasion + d.Masking + d.Sensor <= d.MPCP * 3,
                $"{d.Name}: BEMS sum {d.Bod + d.Evasion + d.Masking + d.Sensor} > 3×MPCP {d.MPCP * 3}");
        });
    }

    [Fact]
    public void ProgramDatabase_LoadsUtilitiesOnly()
    {
        var db = MakeProgramDatabase();
        Assert.NotEmpty(db.AllPrograms);
        // Every program in this phase's catalog must be one of the four utility types.
        var types = db.AllPrograms.Select(p => p.ProgramType).Distinct().ToHashSet();
        Assert.Subset(new HashSet<ProgramType>
        {
            ProgramType.OperationalUtility,
            ProgramType.SpecialUtility,
            ProgramType.OffensiveUtility,
            ProgramType.DefensiveUtility,
        }, types);
    }

    [Fact]
    public void ProgramDatabase_ParsesRatingFromName()
    {
        var db = MakeProgramDatabase();
        var analyze = db.AllPrograms.Where(p =>
            string.Equals(p.CategoryTree.LastOrDefault(), "Analyze", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(analyze);
        // All Analyze ratings should be 1..10 (or similar) — verify rating was parsed, not null.
        Assert.All(analyze, p => Assert.NotNull(p.Rating));
        Assert.Contains(analyze, p => p.Rating >= 1);
    }

    [Fact]
    public void ProgramDatabase_AppliesMultiplierLookup()
    {
        var db = MakeProgramDatabase();
        // Browse: bare archetype, multiplier 1 in the SR3 map.
        var browse = db.AllPrograms.FirstOrDefault(p =>
            string.Equals(p.CategoryTree.LastOrDefault(), "Browse", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(browse);
        Assert.Equal(1, browse!.Multiplier);
    }

    [Fact]
    public void CyberwareDatabase_EncephalonParsesPoolAndScopedIntMods()
    {
        var options = Options.Create(new DatabaseOptions());
        var db = new AugmentationDatabase(options);

        var encephalon2 = db.AllCyberware.FirstOrDefault(c =>
            c.Name.Equals("Encephalon [2]", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(encephalon2);

        // Encephalon 2 from Man & Machine: +2 Hacking Pool, +2 Task Pool, +2 Int for learning
        // new skills (scoped).
        Assert.Contains(encephalon2!.Mods.OfType<DicePoolMod>(),
            m => m.DicePoolType == DicePoolType.Hacking && m.ModValue == 2);
        Assert.Contains(encephalon2.Mods.OfType<DicePoolMod>(),
            m => m.DicePoolType == DicePoolType.Task && m.ModValue == 2);
        Assert.Contains(encephalon2.Mods.OfType<KnowledgeSkillIntMod>(), m => m.ModValue == 2);
    }

    [Fact]
    public void ProgramDatabase_AttackSplitsByDamageCode()
    {
        var db = MakeProgramDatabase();
        // The 40 "Attack Light/Medium/Serious/Deadly" rows should surface as four distinct
        // archetypes with 10 ratings each, not a single Attack archetype with 40 ratings.
        var byName = new Dictionary<string, int>
        {
            ["Attack-L"] = 3, ["Attack-M"] = 4, ["Attack-S"] = 5, ["Attack-D"] = 6,
        };
        foreach (var (archetype, expectedMult) in byName)
        {
            var rows = db.AllPrograms
                .Where(p => string.Equals(p.CategoryTree.LastOrDefault(), archetype, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.Equal(10, rows.Count);
            Assert.All(rows, p => Assert.Equal(expectedMult, p.Multiplier));
        }
        // No rows should remain under the bare "Attack" archetype.
        Assert.DoesNotContain(db.AllPrograms, p =>
            string.Equals(p.CategoryTree.LastOrDefault(), "Attack", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProgramDatabase_ExcludesICAndFramesAndWorms()
    {
        var db = MakeProgramDatabase();
        Assert.DoesNotContain(db.AllPrograms, p =>
            p.CategoryTree.Any(c => c.Contains("IC", StringComparison.OrdinalIgnoreCase)
                                    && c.Length <= 3)); // "IC" as its own branch segment
        Assert.DoesNotContain(db.AllPrograms, p =>
            p.CategoryTree.Any(c => c.Equals("Frames", StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(db.AllPrograms, p =>
            p.CategoryTree.Any(c => c.Equals("Worms", StringComparison.OrdinalIgnoreCase)));
    }
}
