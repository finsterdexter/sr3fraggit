using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SR3Generator.Export;

/// <summary>
/// Renders a <see cref="CharacterSheetModel"/> as a print-friendly, grayscale PDF modelled on the
/// official SR3 Character Record Sheet (core rulebook pp. 337–338): black header bars, boxed
/// sections, a book-style condition monitor. Pure black/white/gray for monochrome printers.
///
/// Page breaks fall on section boundaries: each list section is a table whose black title bar is a
/// repeating header, so it travels with its rows (never orphaned, reprints on continuation pages);
/// bounded blocks (attributes, condition monitor, karma) use ShowEntire so they never split.
/// </summary>
internal sealed class CharacterSheetDocument : IDocument
{
    private enum Align { Left, Center, Right }

    /// <summary>A table column header: label plus the alignment shared with its data cells.</summary>
    private readonly record struct Col(string Label, Align Align = Align.Left);

    private readonly CharacterSheetModel _m;
    private readonly string? _generatedOn;

    public CharacterSheetDocument(CharacterSheetModel model, string? generatedOn = null)
    {
        _m = model;
        _generatedOn = generatedOn;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_m.StreetName} — Shadowrun 3rd Edition character sheet",
        Author = "SR3 Character Generator",
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.MarginHorizontal(34);
            page.MarginVertical(26);
            page.DefaultTextStyle(x => x
                .FontFamily(SheetTheme.SansFont)
                .FontSize(SheetTheme.BodySize)
                .FontColor(SheetTheme.InkPrimary));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(6).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.BorderBottom(1.5f).BorderColor(SheetTheme.InkPrimary).PaddingBottom(4).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_m.StreetName)
                    .FontSize(SheetTheme.TitleSize).Bold().FontColor(SheetTheme.InkPrimary);
                var subtitle = string.Join("   ·   ", new[]
                    {
                        _m.Race,
                        _m.MagicAspect,
                        string.IsNullOrWhiteSpace(_m.RealName) ? null : $"aka {_m.RealName}",
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));
                col.Item().Text(subtitle)
                    .FontSize(SheetTheme.SubtitleSize).FontColor(SheetTheme.InkSecondary);
            });

            row.ConstantItem(130).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("SHADOWRUN")
                    .FontSize(SheetTheme.SubtitleSize).Bold().FontColor(SheetTheme.InkPrimary);
                col.Item().AlignRight().Text("Character Record Sheet · Third Edition")
                    .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
                if (_m.IsFinalized)
                    col.Item().PaddingTop(2).AlignRight().Text("● IN PLAY")
                        .FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkPrimary);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            if (HasProfileFields())
                col.Item().ShowEntire().Element(c => BoxedSection(c, "PROFILE", ComposeProfile));

            // Top grid: Attributes | (Karma + Dice Pools), then Condition Monitor full-width.
            col.Item().ShowEntire().Row(row =>
            {
                row.RelativeItem(5).Element(c => BoxedSection(c, "ATTRIBUTES", ComposeAttributes));
                row.ConstantItem(8);
                row.RelativeItem(4).Column(right =>
                {
                    right.Item().Element(c => BoxedSection(c, "KARMA", ComposeKarma));
                    right.Item().PaddingTop(8).Element(c => BoxedSection(c, "DICE POOLS", ComposeDicePools));
                });
            });

            col.Item().ShowEntire().Element(c => BoxedSection(c, "CONDITION MONITOR", ComposeConditionMonitor));

            if (_m.ActiveSkills.Count > 0)
                col.Item().Element(c => SkillSection(c, "ACTIVE SKILLS", _m.ActiveSkills));

            if (_m.KnowledgeSkills.Count > 0)
                col.Item().Element(c => SkillSection(c, "KNOWLEDGE & LANGUAGE SKILLS", _m.KnowledgeSkills));

            if (_m.IsAwakened)
                ComposeMagicSections(col);

            if (_m.Weapons.Count > 0)
                col.Item().Element(ComposeWeapons);

            if (_m.Armor.Count > 0)
                col.Item().Element(ComposeArmor);

            if (_m.Cyberware.Count > 0)
                col.Item().Element(c => AugSection(c, "CYBERWARE", _m.Cyberware, "Essence"));

            if (_m.Bioware.Count > 0)
                col.Item().Element(c => AugSection(c, "BIOWARE", _m.Bioware, "Bio-Index"));

            if (_m.Gear.Count > 0)
                col.Item().Element(ComposeGear);

            if (_m.HasMatrix)
                ComposeMatrixSections(col);

            if (_m.Vehicles.Count > 0)
                ComposeVehicleSections(col);

            if (_m.Contacts.Count > 0)
                col.Item().Element(ComposeContacts);

            if (_m.EdgesFlaws.Count > 0)
                col.Item().Element(ComposeEdgesFlaws);

            if (_m.Lifestyles.Count > 0)
                col.Item().Element(ComposeLifestyles);

            if (!string.IsNullOrWhiteSpace(_m.Description))
                col.Item().ShowEntire().Element(c => BoxedSection(c, "CHARACTER NOTES",
                    b => b.Text(_m.Description!).FontSize(SheetTheme.BodySize).FontColor(SheetTheme.InkSecondary)));
        });
    }

    // ---- top grid sections ----

    private bool HasProfileFields() =>
        new[] { _m.PlayerName, _m.Gender, _m.Height, _m.Weight, _m.Eyes, _m.Hair }
            .Any(f => !string.IsNullOrWhiteSpace(f)) || _m.Aliases.Count > 0;

    private void ComposeProfile(IContainer container)
    {
        var fields = new List<string>();
        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) fields.Add($"{label}: {value}");
        }
        Add("Player", _m.PlayerName);
        Add("Gender", _m.Gender);
        Add("Height", _m.Height);
        Add("Weight", _m.Weight);
        Add("Eyes", _m.Eyes);
        Add("Hair", _m.Hair);
        if (_m.Aliases.Count > 0) Add("Aliases", string.Join(", ", _m.Aliases));

        container.Text(string.Join("        ", fields))
            .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
    }

    private void ComposeAttributes(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(60);
                });
                foreach (var a in _m.Attributes)
                {
                    table.Cell().Element(DataCell).Text(a.Name)
                        .FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkSecondary);
                    table.Cell().Element(DataCell).AlignRight().Text(text =>
                    {
                        text.Span(a.Base.ToString())
                            .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                        if (a.IsAugmented)
                            text.Span($" ({a.Augmented})")
                                .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    });
                }
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Essence  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    text.Span(_m.EssenceDisplay).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                });
                row.RelativeItem().Text(text =>
                {
                    text.Span("Magic  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    text.Span(_m.Magic.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                });
            });
            col.Item().PaddingTop(2).Text(text =>
            {
                text.Span("Initiative  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                text.Span(_m.InitiativeDisplay).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
            });
        });
    }

    private void ComposeKarma(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Column(k =>
            {
                k.Item().Text("Karma Pool").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                k.Item().Text(_m.KarmaPool.ToString())
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.StatBigSize).Bold();
            });
            col.Item().Column(k =>
            {
                k.Item().Text("Good Karma").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                k.Item().Text(_m.RemainingKarma.ToString())
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.StatBigSize).Bold();
                k.Item().Text($"of {_m.TotalKarma} earned")
                    .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
            });
        });
    }

    private void ComposeDicePools(IContainer container)
    {
        container.Column(col =>
        {
            foreach (var p in _m.DicePools)
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(p.Name).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkSecondary);
                    row.ConstantItem(30).AlignRight().Text(p.Value.ToString())
                        .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                });
            }
        });
    }

    private void ComposeConditionMonitor(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => BoxTrack(c, "Stun", 0));
                row.ConstantItem(16);
                row.RelativeItem().Element(c => BoxTrack(c, "Physical", _m.OverflowBoxes));
            });
            col.Item().PaddingTop(5).Text(
                "1 = L  Light (+1 TN / –1 Init)      3 = M  Moderate (+2 / –2)      " +
                "6 = S  Serious (+3 / –3)      10 = D  Deadly (Unconscious)")
                .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
        });
    }

    private static void BoxTrack(IContainer container, string label, int overflow)
    {
        static string? Mark(int box) => box switch { 1 => "L", 3 => "M", 6 => "S", 10 => "D", _ => null };

        container.Column(col =>
        {
            col.Item().Text(label).FontFamily(SheetTheme.SansFont).FontSize(SheetTheme.SmallSize)
                .Bold().FontColor(SheetTheme.InkSecondary);
            col.Item().PaddingTop(2).Row(row =>
            {
                row.Spacing(2);
                for (var i = 1; i <= 10; i++)
                {
                    row.AutoItem().Column(cell =>
                    {
                        cell.Item().Width(15).Height(13).Border(0.75f).BorderColor(SheetTheme.BoxBorder);
                        cell.Item().Height(8).AlignCenter().Text(Mark(i) ?? "")
                            .FontFamily(SheetTheme.SansFont).FontSize(6).Bold().FontColor(SheetTheme.InkMuted);
                    });
                }
                if (overflow > 0)
                    row.AutoItem().PaddingLeft(8).AlignMiddle().Text($"+{overflow} overflow")
                        .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
            });
        });
    }

    // ---- list sections (repeating black title bar → clean section-boundary breaks) ----

    private void SkillSection(IContainer container, string title, IReadOnlyList<SkillLine> skills)
    {
        TableSection(container, title, 3,
            table => table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(34);
                c.ConstantColumn(44);
            }),
            new[] { new Col("Skill"), new Col("Attr", Align.Center), new Col("Rating", Align.Right) },
            buildRows: table =>
            {
                foreach (var s in skills)
                {
                    table.Cell().Element(DataCell).Text(s.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(s.Attribute)
                        .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                    table.Cell().Element(DataCell).AlignRight().Text(s.Rating.ToString())
                        .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();

                    // Specialization sits on its own indented row directly beneath its base skill.
                    if (!string.IsNullOrWhiteSpace(s.SpecializationName))
                    {
                        table.Cell().Element(DataCell).PaddingLeft(14).Text($"↳ {s.SpecializationName}")
                            .FontSize(SheetTheme.SmallSize).Italic().FontColor(SheetTheme.InkSecondary);
                        table.Cell().Element(DataCell).Text("");
                        table.Cell().Element(DataCell).AlignRight().Text(s.SpecializationRating?.ToString() ?? "")
                            .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    }
                }
            });
    }

    private void ComposeMagicSections(ColumnDescriptor col)
    {
        if (!string.IsNullOrWhiteSpace(_m.Tradition) || _m.MagicNotes.Count > 0)
            col.Item().ShowEntire().Element(c => BoxedSection(c, "MAGIC", ComposeMagicInfo));

        if (_m.Spells.Count > 0)
            col.Item().Element(c => TableSection(c, "SPELLS", 5,
                table => table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(5); cd.ConstantColumn(64); cd.ConstantColumn(34);
                    cd.ConstantColumn(50); cd.RelativeColumn(3);
                }),
                new[] { new Col("Spell"), new Col("Category"), new Col("Force", Align.Center), new Col("Type"), new Col("Drain / Notes") },
                table =>
                {
                    foreach (var s in _m.Spells)
                    {
                        table.Cell().Element(DataCell).Text(s.Name).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).Text(s.Category).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                        table.Cell().Element(DataCell).AlignCenter().Text(s.Force.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).Text(s.Type).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                        table.Cell().Element(DataCell).Text(string.Join("  ", new[] { s.Drain, s.Flags }.Where(x => !string.IsNullOrWhiteSpace(x))))
                            .FontSize(SheetTheme.SmallSize);
                    }
                }));

        if (_m.AdeptPowers.Count > 0)
            col.Item().Element(c => TableSection(c, "ADEPT POWERS", 3,
                table => table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(44); cd.ConstantColumn(50); }),
                new[] { new Col("Power"), new Col("Level", Align.Center), new Col("Points", Align.Right) },
                table =>
                {
                    foreach (var p in _m.AdeptPowers)
                    {
                        table.Cell().Element(DataCell).Text(p.Name).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).AlignCenter().Text(p.Level.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).AlignRight().Text(p.Cost).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                    }
                }));

        if (_m.Foci.Count > 0)
            col.Item().Element(c => TableSection(c, "FOCI", 4,
                table => table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(90); cd.ConstantColumn(40); cd.ConstantColumn(56); }),
                new[] { new Col("Focus"), new Col("Type"), new Col("Rating", Align.Center), new Col("Bonded") },
                table =>
                {
                    foreach (var f in _m.Foci)
                    {
                        table.Cell().Element(DataCell).Text(f.Name).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).Text(f.Type).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                        table.Cell().Element(DataCell).AlignCenter().Text(f.Rating?.ToString() ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).Text(f.IsBound ? "Bonded" : "Unbonded").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                    }
                }));

        if (_m.Spirits.Count > 0)
            col.Item().Element(c => TableSection(c, "SPIRITS", 1,
                table => table.ColumnsDefinition(cd => cd.RelativeColumn()),
                columns: null,
                table =>
                {
                    foreach (var sp in _m.Spirits)
                        table.Cell().Element(DataCell).Text(sp).FontSize(SheetTheme.DataSize);
                }));
    }

    private void ComposeMagicInfo(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(2);
            if (!string.IsNullOrWhiteSpace(_m.Tradition))
                col.Item().Text(_m.Tradition!).FontSize(SheetTheme.DataSize).Bold().FontColor(SheetTheme.InkPrimary);
            foreach (var note in _m.MagicNotes)
                col.Item().Text(note).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
        });
    }

    private void ComposeWeapons(IContainer container)
    {
        TableSection(container, "WEAPONS", 5,
            table => table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(5); c.ConstantColumn(52); c.RelativeColumn(3);
                c.ConstantColumn(72); c.ConstantColumn(48);
            }),
            new[] { new Col("Weapon"), new Col("Damage"), new Col("Modes / Reach"), new Col("Ammo"), new Col("Conceal", Align.Center) },
            table =>
            {
                foreach (var w in _m.Weapons)
                {
                    table.Cell().Element(DataCell).Text(w.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).Text(w.Damage).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    table.Cell().Element(DataCell).Text(w.Detail ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    table.Cell().Element(DataCell).Text(w.Ammo ?? "").FontSize(SheetTheme.SmallSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(w.Conceal ?? "").FontSize(SheetTheme.SmallSize);
                }
            });
    }

    private void ComposeArmor(IContainer container)
    {
        TableSection(container, "ARMOR", 3,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(72); c.ConstantColumn(72); }),
            new[] { new Col("Armor"), new Col("Ballistic", Align.Center), new Col("Impact", Align.Center) },
            table =>
            {
                foreach (var a in _m.Armor)
                {
                    table.Cell().Element(DataCell).Text(a.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(a.Ballistic.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    table.Cell().Element(DataCell).AlignCenter().Text(a.Impact.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                }
            });
    }

    private void AugSection(IContainer container, string title, IReadOnlyList<AugmentationLine> items, string costLabel)
    {
        TableSection(container, title, 4,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(44); c.ConstantColumn(72); c.ConstantColumn(56); }),
            new[] { new Col("Item"), new Col("Rating", Align.Center), new Col("Grade"), new Col(costLabel, Align.Right) },
            table =>
            {
                foreach (var w in items)
                {
                    table.Cell().Element(DataCell).Text(w.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(w.Rating?.ToString() ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).Text(w.Grade).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    table.Cell().Element(DataCell).AlignRight().Text(w.Cost).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                }
            });
    }

    private void ComposeGear(IContainer container)
    {
        TableSection(container, "EQUIPMENT & GEAR", 3,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(4); c.ConstantColumn(36); c.RelativeColumn(4); }),
            new[] { new Col("Item"), new Col("Rtg"), new Col("Notes") },
            table =>
            {
                foreach (var g in _m.Gear)
                {
                    table.Cell().Element(DataCell).Text(g.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).Text(g.Rating is { } r ? $"R{r}" : "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                    table.Cell().Element(DataCell).Text(g.Detail ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                }
            });
    }

    // ---- Matrix (faithful to the Matrix Data Sheet: Persona / Cyberdeck / Matrix Initiative /
    //      Persona Condition Monitor boxes, plus a Utilities table with an Active? column) ----

    private void ComposeMatrixSections(ColumnDescriptor col)
    {
        var multi = _m.MatrixDecks.Count > 1;
        foreach (var d in _m.MatrixDecks)
        {
            var prefix = multi ? $"{d.Name} · " : "";
            if (multi)
                col.Item().PaddingTop(2).Text($"▸ {d.Name}")
                    .FontSize(SheetTheme.SubtitleSize).Bold().FontColor(SheetTheme.InkPrimary);

            col.Item().ShowEntire().Row(row =>
            {
                row.RelativeItem().Element(c => BoxedSection(c, "PERSONA", b => PersonaBody(b, d)));
                row.ConstantItem(8);
                row.RelativeItem().Element(c => BoxedSection(c, "CYBERDECK", b => CyberdeckBody(b, d)));
            });
            col.Item().ShowEntire().Row(row =>
            {
                row.RelativeItem().Element(c => BoxedSection(c, "MATRIX INITIATIVE", b => MatrixInitiativeBody(b, d)));
                row.ConstantItem(8);
                row.RelativeItem().Element(c => BoxedSection(c, "PERSONA CONDITION MONITOR", PersonaConditionBody));
            });

            // Programs split by the memory pool they occupy, each with running used/total — so the
            // player can manage loadouts at a glance instead of decoding a status column.
            var active = d.Utilities.Where(u => u.IsActive).ToList();
            var storage = d.Utilities.Where(u => !u.IsActive).ToList();
            col.Item().Element(c => MemoryTable(c,
                $"{prefix}ACTIVE MEMORY — {d.ActiveMemoryUsed} / {d.ActiveMemoryTotal} Mp", active));
            col.Item().Element(c => MemoryTable(c,
                $"{prefix}STORAGE MEMORY — {d.StorageMemoryUsed} / {d.StorageMemoryTotal} Mp", storage));
        }

        if (_m.CarriedPrograms.Count > 0)
            col.Item().Element(c => MemoryTable(c, "PROGRAMS (CARRIED, NOT LOADED)", _m.CarriedPrograms));
    }

    private static void PersonaBody(IContainer container, MatrixDeckModel d)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(48); c.ConstantColumn(58); });
            // Rating / Effective Rating column labels.
            table.Cell().Element(DataCell).Text("");
            table.Cell().Element(DataCell).AlignRight().Text("Rating").FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkMuted);
            table.Cell().Element(DataCell).AlignRight().Text("Effective").FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkMuted);

            foreach (var (label, value) in new[]
            {
                ("MPCP", d.MPCP), ("Bod", d.Bod), ("Evasion", d.Evasion), ("Masking", d.Masking), ("Sensor", d.Sensor),
            })
            {
                table.Cell().Element(DataCell).Text(label).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkSecondary);
                table.Cell().Element(DataCell).AlignRight().Text(value.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                table.Cell().Element(DataCell).AlignRight().Text("____").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkMuted);
            }
        });
    }

    private static void CyberdeckBody(IContainer container, MatrixDeckModel d) =>
        LabelValueTable(container, 76,
            ("Detection Factor", "______"),
            ("Hardening", d.Hardening.ToString()),
            ("I/O Speed", d.IOSpeed.ToString()),
            ("Response Increase", $"+{d.ResponseIncrease}"),
            ("Active Memory", $"{d.ActiveMemoryUsed}/{d.ActiveMemoryTotal} Mp"),
            ("Storage Memory", $"{d.StorageMemoryUsed}/{d.StorageMemoryTotal} Mp"),
            ("ICCM?", "__ Y / N"),
            ("ASIST", "__ Hot / Cold"),
            ("Reality Filter?", "__ Y / N"));

    private void MatrixInitiativeBody(IContainer container, MatrixDeckModel d)
    {
        // Default: stock deck, jacked in via datajack (pure DNI) running hot ASIST, no reality filter
        // or 'trodes. Reaction = INT +2 (pure DNI/hot ASIST) +2 per Response Increase level;
        // dice = 1 (base) +1 (pure DNI) +1 per Response Increase level.
        var ri = d.ResponseIncrease;
        var intel = _m.MatrixIntelligence;
        var reaction = intel + 2 + 2 * ri;
        var dice = 2 + ri;

        container.Column(col =>
        {
            col.Spacing(2);
            col.Item().Text(text =>
            {
                text.Span("Matrix Initiative  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                text.Span($"{reaction} + {dice}D6").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
            });
            col.Item().Text($"React {reaction} = INT {intel} + 2 pure DNI/hot ASIST"
                + (ri > 0 ? $" + {2 * ri} Response Incr. ×{ri}" : ""))
                .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
            col.Item().Text($"Dice {dice}D6 = 1 base + 1 pure DNI"
                + (ri > 0 ? $" + {ri} Response Incr." : ""))
                .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
            col.Item().PaddingTop(3).Text(
                $"Add if used: reality filter +2/+1D6 · manual controls use physical Reaction ({_m.MatrixPhysicalReaction}) · "
                + "'trodes ÷2 React, max +2D6")
                .FontSize(6f).FontColor(SheetTheme.InkMuted);
        });
    }

    private static void PersonaConditionBody(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => BoxTrack(c, "Persona", 0));
            col.Item().PaddingTop(4).Text("L +1TN/–1Init · M +2/–2 · S +3/–3 · box 10 = persona crashed")
                .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
            col.Item().PaddingTop(4).Text(text =>
            {
                text.Span("Icon Rating  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                text.Span("______").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkMuted);
            });
        });
    }

    /// <summary>One memory pool (Active / Storage / Carried): its programs and sizes, with the pool's
    /// used/total in the black title bar so the player can manage loadouts directly.</summary>
    private static void MemoryTable(IContainer container, string title, IReadOnlyList<MatrixUtilityLine> programs)
    {
        TableSection(container, title, 4,
            table => table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4); c.ConstantColumn(42); c.ConstantColumn(96); c.ConstantColumn(56);
            }),
            new[] { new Col("Program"), new Col("Rating", Align.Center), new Col("Type"), new Col("Size", Align.Right) },
            table =>
            {
                if (programs.Count == 0)
                {
                    table.Cell().Element(DataCell).Text("(none)").FontSize(SheetTheme.SmallSize).Italic().FontColor(SheetTheme.InkMuted);
                    table.Cell().Element(DataCell).Text("");
                    table.Cell().Element(DataCell).Text("");
                    table.Cell().Element(DataCell).Text("");
                    return;
                }
                foreach (var u in programs)
                {
                    table.Cell().Element(DataCell).Text(u.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(u.Rating.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    table.Cell().Element(DataCell).Text(u.Type).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    table.Cell().Element(DataCell).AlignRight().Text($"{u.Size} Mp").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize);
                }
            });
    }

    // ---- Vehicles (Rigger 3 Vehicle Record Sheet: stats / weapons / mods / condition monitor) ----

    private void ComposeVehicleSections(ColumnDescriptor col)
    {
        foreach (var v in _m.Vehicles)
            col.Item().ShowEntire().Element(c => BoxedSection(c, $"VEHICLE · {v.Name}", b => ComposeVehicle(b, v)));
    }

    private void ComposeVehicle(IContainer container, VehicleModel v)
    {
        container.Column(col =>
        {
            col.Spacing(5);
            if (!string.IsNullOrWhiteSpace(v.Type))
                col.Item().Text($"Type: {v.Type}").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);

            col.Item().Text("Control System:   Manual  Y / N      Datajack Port  Y / N      Rigger Adaptation  Y / N      Remote Control  Y / N")
                .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);

            // Initiative used while operating the vehicle: rigged (via VCR) or manual.
            col.Item().Text(text =>
            {
                if (_m.RiggingInitiative is not null)
                {
                    text.Span("Rigging Initiative  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    text.Span(_m.RiggingInitiative).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    text.Span($"   (VCR {_m.VcrRating}: +{2 * _m.VcrRating} Reaction, +{_m.VcrRating}D6 while jumped in; vehicle must be rigger-adapted)")
                        .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
                }
                else
                {
                    text.Span("Initiative  ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    text.Span(_m.InitiativeDisplay).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    text.Span("   (manual control — no VCR installed)").FontSize(6.5f).FontColor(SheetTheme.InkMuted);
                }
            });

            // VEHICLE STATS — three label/value pairs per row.
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    for (var i = 0; i < 3; i++) { c.RelativeColumn(3); c.RelativeColumn(2); }
                });
                foreach (var s in v.Stats)
                {
                    table.Cell().Element(DataCell).Text(s.Label).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                    table.Cell().Element(DataCell).Text(s.Value).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                }
                var pad = (3 - v.Stats.Count % 3) % 3;
                for (var i = 0; i < pad * 2; i++)
                    table.Cell().Element(DataCell).Text("");
            });

            if (v.Weapons.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Vehicle Weapons"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4); c.ConstantColumn(56); c.ConstantColumn(60); c.ConstantColumn(48);
                    });
                    foreach (var (label, align) in new[] { ("Weapon", Align.Left), ("Mode", Align.Left), ("Ammo", Align.Left), ("Damage", Align.Left) })
                        Aligned(table.Cell().Background(SheetTheme.SubtleTint).PaddingVertical(2).PaddingHorizontal(4), align)
                            .Text(label).FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkSecondary);
                    foreach (var w in v.Weapons)
                    {
                        table.Cell().Element(DataCell).Text(w.Mount == w.Weapon ? w.Weapon : $"{w.Weapon} ({w.Mount})").FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataCell).Text(w.Modes).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                        table.Cell().Element(DataCell).Text(w.Ammo).FontSize(SheetTheme.SmallSize);
                        table.Cell().Element(DataCell).Text(w.Damage).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    }
                });
            }

            if (v.Mods.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Modifications / Notes"));
                foreach (var mod in v.Mods)
                    col.Item().Text($"• {mod}").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
            }

            col.Item().PaddingTop(2).Element(c => BoxTrack(c, "Damage", 0));
            col.Item().PaddingTop(3).Text(
                "L +1TN/–1Init · M +2/–2 (25% speed) · S +3/–3 (50% speed) · box 10 = Destroyed")
                .FontSize(6.5f).FontColor(SheetTheme.InkMuted);
        });
    }

    /// <summary>A simple two-column label/value table (used by the Cyberdeck box).</summary>
    private static void LabelValueTable(IContainer container, float valueWidth, params (string Label, string Value)[] rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(valueWidth); });
            foreach (var (label, value) in rows)
            {
                table.Cell().Element(DataCell).Text(label).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkSecondary);
                table.Cell().Element(DataCell).AlignRight().Text(value).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
            }
        });
    }

    private void ComposeContacts(IContainer container)
    {
        TableSection(container, "CONTACTS & INFORMATION", 4,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); c.RelativeColumn(); c.ConstantColumn(90); }),
            new[] { new Col("Contact"), new Col("Level"), new Col("Contact"), new Col("Level") },
            table =>
            {
                foreach (var ct in _m.Contacts)
                {
                    table.Cell().Element(DataCell).Text(ct.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).Text(ct.Level).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                }
                if (_m.Contacts.Count % 2 == 1)
                    for (var i = 0; i < 2; i++)
                        table.Cell().Element(DataCell).Text("");
            });
    }

    private void ComposeEdgesFlaws(IContainer container)
    {
        TableSection(container, "EDGES & FLAWS", 3,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(4); c.ConstantColumn(44); c.RelativeColumn(4); }),
            new[] { new Col("Edge / Flaw"), new Col("Points", Align.Center), new Col("Notes") },
            table =>
            {
                foreach (var ef in _m.EdgesFlaws)
                {
                    table.Cell().Element(DataCell).Text(ef.Name).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text((ef.Points > 0 ? "+" : "") + ef.Points)
                        .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    table.Cell().Element(DataCell).Text(ef.Notes ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                }
            });
    }

    private void ComposeLifestyles(IContainer container)
    {
        TableSection(container, "LIFESTYLES", 3,
            table => table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); c.ConstantColumn(70); }),
            new[] { new Col("Lifestyle"), new Col("Monthly ¥", Align.Right), new Col("Months", Align.Center) },
            table =>
            {
                foreach (var l in _m.Lifestyles)
                {
                    table.Cell().Element(DataCell).Text(l.Tier).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignRight().Text(l.MonthlyCost.ToString("N0")).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                    table.Cell().Element(DataCell).AlignCenter().Text(l.MonthsPaid.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                }
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(SheetTheme.InkPrimary).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Nuyen ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                text.Span(_m.NuyenRemaining).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).Bold();
                text.Span("      Karma ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                text.Span($"{_m.RemainingKarma} left / {_m.TotalKarma} total")
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize);
            });

            if (!string.IsNullOrWhiteSpace(_generatedOn))
                row.RelativeItem().AlignCenter().Text(_generatedOn!)
                    .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    // ---- shared building blocks ----

    /// <summary>A bordered box with a black title bar — kept intact by the caller via ShowEntire.</summary>
    private static void BoxedSection(IContainer container, string title, Action<IContainer> body)
    {
        container.Border(0.75f).BorderColor(SheetTheme.BoxBorder).Column(col =>
        {
            col.Item().Background(SheetTheme.HeaderBarBg).PaddingVertical(3).PaddingHorizontal(6)
                .Text(title).FontFamily(SheetTheme.SansFont).FontSize(SheetTheme.SectionHeaderSize)
                .Bold().FontColor(SheetTheme.HeaderBarText);
            col.Item().Padding(6).Element(body);
        });
    }

    /// <summary>
    /// A list section whose black title bar is the table's repeating header, so the section header
    /// travels with its rows: it is never left orphaned at a page bottom, and reprints atop any
    /// continuation page. Bordered like the record sheet's boxed sections.
    /// </summary>
    private static void TableSection(IContainer container, string title, uint columnCount,
        Action<TableDescriptor> defineColumns, Col[]? columns, Action<TableDescriptor> buildRows)
    {
        container.Border(0.75f).BorderColor(SheetTheme.BoxBorder).Table(table =>
        {
            defineColumns(table);
            table.Header(header =>
            {
                header.Cell().ColumnSpan(columnCount)
                    .Background(SheetTheme.HeaderBarBg).PaddingVertical(3).PaddingHorizontal(6)
                    .Text(title).FontFamily(SheetTheme.SansFont).FontSize(SheetTheme.SectionHeaderSize)
                    .Bold().FontColor(SheetTheme.HeaderBarText);
                if (columns is not null)
                    foreach (var col in columns)
                        Aligned(header.Cell().Background(SheetTheme.SubtleTint).PaddingVertical(2).PaddingHorizontal(4), col.Align)
                            .Text(col.Label).FontFamily(SheetTheme.SansFont).FontSize(SheetTheme.SmallSize)
                            .Bold().FontColor(SheetTheme.InkSecondary);
            });
            buildRows(table);
        });
    }

    private static IContainer Aligned(IContainer c, Align align) => align switch
    {
        Align.Center => c.AlignCenter(),
        Align.Right => c.AlignRight(),
        _ => c,
    };

    /// <summary>A light sub-heading used inside a boxed section (e.g. "Vehicle Weapons").</summary>
    private static void SubHeader(IContainer container, string title) =>
        container.Text(title).FontFamily(SheetTheme.SansFont).FontSize(SheetTheme.SmallSize)
            .Bold().FontColor(SheetTheme.InkSecondary);

    private static IContainer DataCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(SheetTheme.HairLine).PaddingVertical(1.5f).PaddingHorizontal(4);
}
