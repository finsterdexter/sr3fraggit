using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SR3Generator.Export;

/// <summary>
/// Renders a <see cref="CharacterSheetModel"/> as a print-friendly, multi-page PDF using QuestPDF.
/// Layout is a single stacked column of sections that flow across pages as content grows; each
/// section only renders when it has content. Pure projection — no SR3 rules logic lives here.
/// </summary>
internal sealed class CharacterSheetDocument : IDocument
{
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
        container.BorderBottom(1.5f).BorderColor(SheetTheme.Cyber).PaddingBottom(4).Row(row =>
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

            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("SHADOWRUN")
                    .FontSize(SheetTheme.SubtitleSize).Bold().FontColor(SheetTheme.Cyber);
                col.Item().AlignRight().Text("Third Edition")
                    .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                if (_m.IsFinalized)
                    col.Item().AlignRight().Text("● IN PLAY")
                        .FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.Karma);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(9);

            // Identity meta line + physical description.
            col.Item().Element(ComposeMeta);

            // Attributes + derived stats side by side (both short — fit on page one).
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => Section(c, "ATTRIBUTES", SheetTheme.Cyber, ComposeAttributes));
                row.ConstantItem(14);
                row.RelativeItem(4).Element(c => Section(c, "DERIVED", SheetTheme.Cyber, ComposeDerived));
            });

            col.Item().Element(c => Section(c, "CONDITION MONITOR", SheetTheme.Cyber, ComposeConditionMonitor));

            if (_m.ActiveSkills.Count > 0)
                col.Item().Element(c => Section(c, "ACTIVE SKILLS", SheetTheme.Cyber,
                    b => TwoColumnSkills(b, _m.ActiveSkills)));

            if (_m.KnowledgeSkills.Count > 0)
                col.Item().Element(c => Section(c, "KNOWLEDGE & LANGUAGE SKILLS", SheetTheme.Cyber,
                    b => TwoColumnSkills(b, _m.KnowledgeSkills)));

            if (_m.IsAwakened)
                col.Item().Element(c => Section(c, "MAGIC", SheetTheme.Mana, ComposeMagic));

            if (_m.Weapons.Count > 0)
                col.Item().Element(c => Section(c, "WEAPONS", SheetTheme.Cyber, ComposeWeapons));

            if (_m.Armor.Count > 0)
                col.Item().Element(c => Section(c, "ARMOR", SheetTheme.Cyber, ComposeArmor));

            if (_m.Cyberware.Count > 0 || _m.Bioware.Count > 0)
                col.Item().Element(c => Section(c, "AUGMENTATIONS", SheetTheme.Cyber, ComposeAugmentations));

            if (_m.Gear.Count > 0)
                col.Item().Element(c => Section(c, "GEAR", SheetTheme.Cyber, ComposeGear));

            if (_m.Vehicles.Count > 0)
                col.Item().Element(c => Section(c, "VEHICLES & DRONES", SheetTheme.Cyber, ComposeVehicles));

            if (_m.Contacts.Count > 0)
                col.Item().Element(c => Section(c, "CONTACTS", SheetTheme.Cyber, ComposeContacts));

            if (_m.EdgesFlaws.Count > 0)
                col.Item().Element(c => Section(c, "EDGES & FLAWS", SheetTheme.Cyber, ComposeEdgesFlaws));

            if (_m.Lifestyles.Count > 0)
                col.Item().Element(c => Section(c, "LIFESTYLES", SheetTheme.Nuyen, ComposeLifestyles));

            if (!string.IsNullOrWhiteSpace(_m.Description))
                col.Item().Element(c => Section(c, "NOTES", SheetTheme.Cyber,
                    b => b.Text(_m.Description!).FontSize(SheetTheme.BodySize).FontColor(SheetTheme.InkSecondary)));
        });
    }

    private void ComposeMeta(IContainer container)
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

        container.Background(SheetTheme.SurfaceTint).Padding(6).Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary));
            if (fields.Count == 0)
            {
                text.Span("No additional details recorded.").Italic();
                return;
            }
            text.Span(string.Join("      ", fields));
        });
    }

    private void ComposeAttributes(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();       // name
                c.ConstantColumn(56);     // value
            });
            foreach (var a in _m.Attributes)
            {
                table.Cell().Element(DataRowCell).Text(a.Name)
                    .FontSize(SheetTheme.DataSize).FontColor(SheetTheme.InkSecondary);
                table.Cell().Element(DataRowCell).AlignRight().Text(text =>
                {
                    text.Span(a.Base.ToString())
                        .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    if (a.IsAugmented)
                        text.Span($" ({a.Augmented})")
                            .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.Cyber);
                });
            }
        });
    }

    private void ComposeDerived(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => StatBlock(c, "ESSENCE", _m.EssenceDisplay, SheetTheme.Cyber));
                row.RelativeItem().Element(c => StatBlock(c, "MAGIC", _m.Magic.ToString(), SheetTheme.Mana));
                row.RelativeItem().Element(c => StatBlock(c, "KARMA POOL", _m.KarmaPool.ToString(), SheetTheme.Karma));
            });

            col.Item().PaddingTop(4).Text(text =>
            {
                text.Span("Initiative   ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                text.Span(_m.InitiativeDisplay).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
            });

            col.Item().PaddingTop(4).Text("DICE POOLS")
                .FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkMuted);
            col.Item().Row(row =>
            {
                foreach (var p in _m.DicePools)
                {
                    row.AutoItem().PaddingRight(10).Text(text =>
                    {
                        text.Span($"{p.Name} ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                        text.Span(p.Value.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                    });
                }
            });
        });
    }

    private void ComposeConditionMonitor(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => BoxTrack(c, "Physical", _m.PhysicalBoxes, _m.OverflowBoxes));
            row.ConstantItem(20);
            row.RelativeItem().Element(c => BoxTrack(c, "Stun", _m.StunBoxes, 0));
        });
    }

    private static void BoxTrack(IContainer container, string label, int boxes, int overflow)
    {
        // SR3 wound levels on a 10-box track: Light@1 (+1), Moderate@3 (+2), Serious@6 (+3), Deadly@10.
        string? WoundLabel(int box) => box switch
        {
            1 => "L +1",
            3 => "M +2",
            6 => "S +3",
            10 => "D",
            _ => null,
        };

        container.Column(col =>
        {
            col.Item().Text(label).FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkSecondary);
            col.Item().PaddingTop(2).Row(row =>
            {
                row.Spacing(2);
                for (var i = 1; i <= boxes; i++)
                {
                    var wl = WoundLabel(i);
                    row.AutoItem().Column(cell =>
                    {
                        cell.Item().Width(13).Height(13).Border(1).BorderColor(SheetTheme.BorderStrong);
                        cell.Item().Height(9).Text(wl ?? "")
                            .FontSize(6).FontColor(SheetTheme.InkMuted);
                    });
                }
                if (overflow > 0)
                {
                    row.AutoItem().PaddingLeft(6).Column(cell =>
                    {
                        cell.Item().Text($"+{overflow} overflow")
                            .FontSize(6).FontColor(SheetTheme.InkMuted);
                    });
                }
            });
        });
    }

    private void TwoColumnSkills(IContainer container, IReadOnlyList<SkillLine> skills)
    {
        container.Table(table =>
        {
            // Two logical skill columns, each: name | attr | rating.
            table.ColumnsDefinition(c =>
            {
                for (var i = 0; i < 2; i++)
                {
                    c.RelativeColumn(6);   // name (+ spec)
                    c.ConstantColumn(28);  // attr
                    c.ConstantColumn(24);  // rating
                }
            });
            foreach (var s in skills)
            {
                table.Cell().Element(DataRowCell).Text(text =>
                {
                    text.Span(s.Name).FontSize(SheetTheme.DataSize);
                    if (!string.IsNullOrWhiteSpace(s.Specialization))
                        text.Span($"  ({s.Specialization})").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                });
                table.Cell().Element(DataRowCell).Text(s.Attribute)
                    .FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                table.Cell().Element(DataRowCell).AlignRight().Text(s.Rating.ToString())
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
            }
            // Pad an odd trailing cell so the last row's grid stays aligned.
            if (skills.Count % 2 == 1)
            {
                table.Cell().Element(DataRowCell).Text("");
                table.Cell().Element(DataRowCell).Text("");
                table.Cell().Element(DataRowCell).Text("");
            }
        });
    }

    private void ComposeMagic(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            if (!string.IsNullOrWhiteSpace(_m.Tradition))
                col.Item().Text(_m.Tradition!).FontSize(SheetTheme.DataSize).FontColor(SheetTheme.Mana).Bold();

            if (_m.Spells.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Spells"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(5); c.ConstantColumn(60); c.ConstantColumn(30);
                        c.ConstantColumn(48); c.RelativeColumn(3);
                    });
                    HeaderRow(table, "Spell", "Category", "F", "Type", "Drain / Notes");
                    foreach (var s in _m.Spells)
                    {
                        table.Cell().Element(DataRowCell).Text(s.Name).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataRowCell).Text(s.Category).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                        table.Cell().Element(DataRowCell).Text(s.Force.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataRowCell).Text(s.Type).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                        table.Cell().Element(DataRowCell).Text(string.Join("  ", new[] { s.Drain, s.Flags }.Where(x => !string.IsNullOrWhiteSpace(x))))
                            .FontSize(SheetTheme.SmallSize);
                    }
                });
            }

            if (_m.AdeptPowers.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Adept Powers"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(40); c.ConstantColumn(48); });
                    HeaderRow(table, "Power", "Level", "Points");
                    foreach (var p in _m.AdeptPowers)
                    {
                        table.Cell().Element(DataRowCell).Text(p.Name).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataRowCell).Text(p.Level.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                        table.Cell().Element(DataRowCell).AlignRight().Text(p.Cost).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                    }
                });
            }

            if (_m.Foci.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Foci"));
                foreach (var f in _m.Foci)
                    col.Item().Text(text =>
                    {
                        text.Span(f.Name).FontSize(SheetTheme.DataSize);
                        var meta = $"{f.Type}{(f.Rating is { } r ? $" R{r}" : "")} · {(f.IsBound ? "bonded" : "unbonded")}";
                        text.Span($"   {meta}").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                    });
            }

            if (_m.Spirits.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Spirits"));
                foreach (var sp in _m.Spirits)
                    col.Item().Text(sp).FontSize(SheetTheme.DataSize);
            }
        });
    }

    private void ComposeWeapons(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(5); c.ConstantColumn(48); c.RelativeColumn(3);
                c.ConstantColumn(70); c.ConstantColumn(46);
            });
            HeaderRow(table, "Weapon", "Damage", "Modes / Reach", "Ammo", "Conceal");
            foreach (var w in _m.Weapons)
            {
                table.Cell().Element(DataRowCell).Text(w.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).Text(w.Damage).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                table.Cell().Element(DataRowCell).Text(w.Detail ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                table.Cell().Element(DataRowCell).Text(w.Ammo ?? "").FontSize(SheetTheme.SmallSize);
                table.Cell().Element(DataRowCell).Text(w.Conceal ?? "").FontSize(SheetTheme.SmallSize);
            }
        });
    }

    private void ComposeArmor(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(70); c.ConstantColumn(70); });
            HeaderRow(table, "Armor", "Ballistic", "Impact");
            foreach (var a in _m.Armor)
            {
                table.Cell().Element(DataRowCell).Text(a.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(a.Ballistic.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
                table.Cell().Element(DataRowCell).AlignCenter().Text(a.Impact.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold();
            }
        });
    }

    private void ComposeAugmentations(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            if (_m.Cyberware.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Cyberware"));
                col.Item().Element(c => AugTable(c, _m.Cyberware, "Essence"));
            }
            if (_m.Bioware.Count > 0)
            {
                col.Item().Element(c => SubHeader(c, "Bioware"));
                col.Item().Element(c => AugTable(c, _m.Bioware, "Bio-Index"));
            }
        });
    }

    private static void AugTable(IContainer container, IReadOnlyList<AugmentationLine> items, string costLabel)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(); c.ConstantColumn(40); c.ConstantColumn(70); c.ConstantColumn(56);
            });
            HeaderRow(table, "Item", "Rating", "Grade", costLabel);
            foreach (var w in items)
            {
                table.Cell().Element(DataRowCell).Text(w.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).Text(w.Rating?.ToString() ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).Text(w.Grade).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
                table.Cell().Element(DataRowCell).AlignRight().Text(w.Cost).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
            }
        });
    }

    private void ComposeGear(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4); c.ConstantColumn(36); c.RelativeColumn(4);
            });
            foreach (var g in _m.Gear)
            {
                table.Cell().Element(DataRowCell).Text(g.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).Text(g.Rating is { } r ? $"R{r}" : "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                table.Cell().Element(DataRowCell).Text(g.Detail ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
            }
        });
    }

    private void ComposeVehicles(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(); c.ConstantColumn(56); c.ConstantColumn(56); c.ConstantColumn(48); c.ConstantColumn(48);
            });
            HeaderRow(table, "Vehicle / Drone", "Handling", "Speed", "Body", "Armor");
            foreach (var v in _m.Vehicles)
            {
                table.Cell().Element(DataRowCell).Text(v.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(v.Handling ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(v.Speed ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(v.Body?.ToString() ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(v.Armor ?? "").FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
            }
        });
    }

    private void ComposeContacts(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); c.RelativeColumn(); c.ConstantColumn(90); });
            foreach (var ct in _m.Contacts)
            {
                table.Cell().Element(DataRowCell).Text(ct.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).Text(ct.Level).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkSecondary);
            }
            if (_m.Contacts.Count % 2 == 1)
            {
                table.Cell().Element(DataRowCell).Text("");
                table.Cell().Element(DataRowCell).Text("");
            }
        });
    }

    private void ComposeEdgesFlaws(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(4); c.ConstantColumn(40); c.RelativeColumn(4); });
            HeaderRow(table, "Edge / Flaw", "Points", "Notes");
            foreach (var ef in _m.EdgesFlaws)
            {
                var color = ef.Kind == "Edge" ? SheetTheme.Karma : SheetTheme.Nuyen;
                table.Cell().Element(DataRowCell).Text(ef.Name).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text((ef.Points > 0 ? "+" : "") + ef.Points)
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize).Bold().FontColor(color);
                table.Cell().Element(DataRowCell).Text(ef.Notes ?? "").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
            }
        });
    }

    private void ComposeLifestyles(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); c.ConstantColumn(70); });
            HeaderRow(table, "Lifestyle", "Monthly ¥", "Months");
            foreach (var l in _m.Lifestyles)
            {
                table.Cell().Element(DataRowCell).Text(l.Tier).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignRight().Text(l.MonthlyCost.ToString("N0")).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
                table.Cell().Element(DataRowCell).AlignCenter().Text(l.MonthsPaid.ToString()).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.DataSize);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(SheetTheme.Border).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Nuyen ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                text.Span(_m.NuyenRemaining).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.Nuyen);
                text.Span("      Karma ").FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
                text.Span($"{_m.RemainingKarma} left / {_m.TotalKarma} total")
                    .FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.Karma);
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

    private static void Section(IContainer container, string title, string accent, Action<IContainer> body)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(accent).PaddingBottom(2).Text(title)
                .FontSize(SheetTheme.SectionHeaderSize).Bold().FontColor(accent);
            col.Item().PaddingTop(4).Element(body);
        });
    }

    private static void SubHeader(IContainer container, string title) =>
        container.PaddingTop(2).Text(title)
            .FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkSecondary);

    private static void StatBlock(IContainer container, string label, string value, string accent)
    {
        container.Border(1).BorderColor(SheetTheme.Border).Padding(4).Column(col =>
        {
            col.Item().Text(label).FontSize(SheetTheme.SmallSize).FontColor(SheetTheme.InkMuted);
            col.Item().Text(value).FontFamily(SheetTheme.MonoFont).FontSize(SheetTheme.StatBigSize).Bold().FontColor(accent);
        });
    }

    private static IContainer DataRowCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(SheetTheme.Border).PaddingVertical(1.5f).PaddingRight(4);

    private static void HeaderRow(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
            table.Cell().Background(SheetTheme.SurfaceTint).PaddingVertical(2).PaddingHorizontal(3)
                .Text(h).FontSize(SheetTheme.SmallSize).Bold().FontColor(SheetTheme.InkSecondary);
    }
}
