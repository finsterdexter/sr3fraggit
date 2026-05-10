using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SR3Generator.Creation.Test;

public class AttachmentSystemTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new JsonStringEnumConverter() },
    };

    private static Cyberware MakeLimb(decimal capacity, string name = "Cyberarm") => new()
    {
        Name = name,
        Book = "M&M",
        Availability = new Availability { TargetNumber = 0, Interval = "Always" },
        Capacity = capacity,
    };

    private static Cyberware MakeChild(decimal capacityCost, string name = "Smartlink") => new()
    {
        Name = name,
        Book = "M&M",
        Availability = new Availability { TargetNumber = 0, Interval = "Always" },
        Capacity = capacityCost, // child's own capacity rating; cost is on the slot
    };

    private static Vehicle MakeVehicle(int body = 4, int cargo = 10, int load = 200) => new()
    {
        Name = "Test Sedan",
        Book = "Rigger 3",
        Availability = new Availability { TargetNumber = 0, Interval = "Always" },
        Body = body,
        Cargo = cargo,
        Load = load,
    };

    private static Firearm MakeFirearm(int top = 1, int barrel = 1, int under = 1, int internalSlots = 0) => new()
    {
        Name = "Ares Predator",
        Book = "SR3",
        Availability = new Availability { TargetNumber = 0, Interval = "Always" },
        Skill = "Pistols",
        Damage = "9M",
        Ammo = new AmmunitionLoad { Rounds = 12, Type = ReloadType.Clip },
        TopMountSlots = top,
        BarrelMountSlots = barrel,
        UnderMountSlots = under,
        InternalMountSlots = internalSlots,
    };

    private static Cyberdeck MakeDeck(int activeMem = 200, int storageMem = 400) => new()
    {
        Name = "Fairlight",
        Book = "Matrix",
        Availability = new Availability { TargetNumber = 0, Interval = "Always" },
        ActiveMemory = activeMem,
        StorageMemory = storageMem,
    };

    // ---------- Capacity arithmetic (cyberware) ----------

    [Fact]
    public void Cyberlimb_AtExactCapacity_NoFailures()
    {
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 3m });
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 2m });

        Assert.Empty(AttachmentValidator.Validate(limb));
    }

    [Fact]
    public void Cyberlimb_OverCapacity_OneFailureWithCorrectNumbers()
    {
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 5.1m });

        var failures = AttachmentValidator.Validate(limb);
        var f = Assert.Single(failures);
        Assert.Equal(CapacityKind.CyberwareCapacity, f.Kind);
        Assert.Equal(5m, f.Total);
        Assert.Equal(5.1m, f.Used);
    }

    [Fact]
    public void Cyberlimb_Empty_NoFailuresRegardlessOfCapacityRating()
    {
        Assert.Empty(AttachmentValidator.Validate(MakeLimb(0m)));
        Assert.Empty(AttachmentValidator.Validate(MakeLimb(8m)));
    }

    // ---------- Multi-bucket host (cyberdeck) ----------

    [Fact]
    public void Cyberdeck_BucketsScoredIndependently_StorageOverrun_DoesNotFlagActive()
    {
        var deck = MakeDeck(activeMem: 200, storageMem: 100);
        deck.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.ProgramActiveMemory, CapacityCost = 50, GearReferenceId = Guid.NewGuid() });
        deck.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.ProgramStorageMemory, CapacityCost = 150, GearReferenceId = Guid.NewGuid() });

        var failures = AttachmentValidator.Validate(deck);
        var f = Assert.Single(failures);
        Assert.Equal(CapacityKind.ProgramStorageMemory, f.Kind);
    }

    // ---------- Mount-position math (firearm) ----------

    [Fact]
    public void Firearm_TwoTopAccessories_OnOneTopMount_Fails()
    {
        var gun = MakeFirearm(top: 1, barrel: 1, under: 1);
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "Top", CapacityCost = 1 });
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "top", CapacityCost = 1 });

        var failures = AttachmentValidator.Validate(gun);
        Assert.Contains(failures, f => f.Message.Contains("Top mount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Firearm_PerPositionDistributionExceeded_StillFails_EvenIfOverallSumOk()
    {
        // overall = 3, per-Top = 1; place 2 on Top — overall 2 ≤ 3 but Top 2 > 1
        var gun = MakeFirearm(top: 1, barrel: 1, under: 1);
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "Top", CapacityCost = 1 });
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "Top", CapacityCost = 1 });

        var failures = AttachmentValidator.Validate(gun);
        Assert.NotEmpty(failures);
    }

    [Fact]
    public void Firearm_SpecialtyMount_PassesPerPositionCheck_ButCountsAgainstOverallBucket()
    {
        // overall = 1; specialty Grips slot consumes that 1.
        var gun = MakeFirearm(top: 1, barrel: 0, under: 0);
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "Grips", CapacityCost = 1 });
        Assert.Empty(AttachmentValidator.Validate(gun));

        // Add a second specialty slot — overall 2 > 1.
        gun.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "3-Lug", CapacityCost = 1 });
        var failures = AttachmentValidator.Validate(gun);
        Assert.Contains(failures, f => f.Kind == CapacityKind.FirearmMount && f.Total == 1);
    }

    // ---------- Vehicle capacity ----------

    [Fact]
    public void Vehicle_DualBucketMod_TwoSlotsSharingOneEmbedded_FlagsBothBucketsIndependently()
    {
        var v = MakeVehicle(body: 4, cargo: 5, load: 50);
        var embedded = new Equipment { Name = "Heavy Mod", Book = "R3", Availability = v.Availability };
        // CF over, Load fine.
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleCargoCF, CapacityCost = 6, Embedded = embedded });
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleLoadKg,  CapacityCost = 25, Embedded = embedded });

        var failures = AttachmentValidator.Validate(v);
        var f = Assert.Single(failures);
        Assert.Equal(CapacityKind.VehicleCargoCF, f.Kind);
    }

    [Fact]
    public void Vehicle_LoadTrackEngineMod_BoostsLoadCapacity()
    {
        var v = MakeVehicle(body: 4, load: 100);

        // Without the engine mod, a 250 kg load mod would be over capacity.
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleLoadKg, CapacityCost = 250 });
        Assert.NotEmpty(AttachmentValidator.Validate(v));

        // Two Load-track engine levels add Body × 50 × 2 = 400 kg → total 500 kg.
        v.Attachments.Insert(0, new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 0,
            VehicleCategory = VehicleModCategory.Engine,
            EngineTrack = EngineCustomizationTrack.Load,
        });
        v.Attachments.Insert(0, new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 0,
            VehicleCategory = VehicleModCategory.Engine,
            EngineTrack = EngineCustomizationTrack.Load,
        });
        Assert.Empty(AttachmentValidator.Validate(v));
    }

    [Fact]
    public void Vehicle_SpeedTrackEngineMod_DoesNotBoostLoad()
    {
        var v = MakeVehicle(body: 4, load: 100);
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleLoadKg, CapacityCost = 250 });
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 0,
            VehicleCategory = VehicleModCategory.Engine,
            EngineTrack = EngineCustomizationTrack.Speed,
        });
        // Speed-track must not contribute to Load — so 250 > 100 still fails.
        Assert.NotEmpty(AttachmentValidator.Validate(v));
    }

    [Fact]
    public void Vehicle_HardpointConsumes2_Firmpoint1_OverBodyFails()
    {
        var v = MakeVehicle(body: 3); // 3 mount points available
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleMountPoints, IsVehicleHardpoint = true,  CapacityCost = 2, VehicleCategory = VehicleModCategory.WeaponMount });
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleMountPoints, IsVehicleHardpoint = false, CapacityCost = 1, VehicleCategory = VehicleModCategory.WeaponMount });
        Assert.Empty(AttachmentValidator.Validate(v)); // 2+1=3, exact

        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleMountPoints, IsVehicleHardpoint = false, CapacityCost = 1, VehicleCategory = VehicleModCategory.WeaponMount });
        var failures = AttachmentValidator.Validate(v);
        Assert.Contains(failures, f => f.Kind == CapacityKind.VehicleMountPoints && f.Total == 3 && f.Used == 4);
    }

    [Fact]
    public void Vehicle_BodyScaledCost_FrozenAtAttachTime_NotRecomputedFromCurrentBody()
    {
        // Smart Armor on a Body 5 vehicle = 250 kg Load (Rigger 3 p. 134).
        // Slot CapacityCost is the authoritative install-time value — if we
        // change the host's Body afterward, the slot cost should not change.
        var v = MakeVehicle(body: 5, load: 300);
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 250m, // 5 × 50
            VehicleCategory = VehicleModCategory.ProtectiveSystems,
        });
        Assert.Empty(AttachmentValidator.Validate(v));

        v.Body = 1; // pretend the host's Body got nerfed somehow
        // Slot is still 250 kg; 250 ≤ 300 so still fits. (If slot recomputed
        // from new Body, it'd be 50 — also fits but for the wrong reason. The
        // test pins the "frozen" behavior, not the value.)
        Assert.Empty(AttachmentValidator.Validate(v));
        Assert.Equal(250m, v.Attachments[0].CapacityCost);
    }

    // ---------- Recursion ----------

    [Fact]
    public void Cyberlimb_NestedSmartlinkOverCapacity_FlaggedAtSmartlinkLevel_NotLimb()
    {
        var smartlink = MakeChild(capacityCost: 1m, name: "Smartlink");
        smartlink.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 2m }); // > 1

        var limb = MakeLimb(capacity: 5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 1m, Embedded = smartlink });

        var failures = AttachmentValidator.Validate(limb);
        var f = Assert.Single(failures);
        Assert.Same(smartlink, f.Host); // surfaces against the inner host, not the limb
    }

    [Fact]
    public void Walker_DualBucketSlotsSharingOneEmbedded_DescendsOnce_NotTwice()
    {
        // If the walker descended twice into a shared Embedded host, an inner
        // failure would be reported twice. Pin once-and-only-once.
        var inner = MakeLimb(capacity: 1m);
        inner.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 2m });

        var v = MakeVehicle(body: 5, cargo: 10, load: 100);
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleCargoCF, CapacityCost = 1, Embedded = inner });
        v.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleLoadKg, CapacityCost = 5, Embedded = inner });

        var failures = AttachmentValidator.Validate(v);
        // Exactly one failure from the shared inner host.
        Assert.Single(failures, f => f.Host == inner);
    }

    // ---------- Polymorphic round-trip ----------

    [Fact]
    public void RoundTrip_Vehicle_WithDualBucketSlots_PreservesAllSlotFields()
    {
        var v = MakeVehicle(body: 4, cargo: 8, load: 200);
        var mod = new Equipment { Name = "Smart Armor", Book = "R3", Availability = v.Availability };
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleCargoCF,
            CapacityCost = 2m,
            Embedded = mod,
            VehicleCategory = VehicleModCategory.ProtectiveSystems,
            Notes = "smart-armor",
        });
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 200m,
            Embedded = mod,
            VehicleCategory = VehicleModCategory.ProtectiveSystems,
        });
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleMountPoints,
            CapacityCost = 2m,
            IsVehicleHardpoint = true,
            VehicleCategory = VehicleModCategory.WeaponMount,
        });
        v.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleLoadKg,
            CapacityCost = 0m,
            VehicleCategory = VehicleModCategory.Engine,
            EngineTrack = EngineCustomizationTrack.Load,
        });

        // Equipment is polymorphic — round-trip via the Equipment base type.
        var json = JsonSerializer.Serialize<Equipment>(v, JsonOptions);
        var restored = JsonSerializer.Deserialize<Equipment>(json, JsonOptions) as Vehicle;

        Assert.NotNull(restored);
        Assert.Equal(4, restored!.Attachments.Count);
        Assert.Equal(CapacityKind.VehicleCargoCF, restored.Attachments[0].Kind);
        Assert.Equal(VehicleModCategory.ProtectiveSystems, restored.Attachments[0].VehicleCategory);
        Assert.Equal("smart-armor", restored.Attachments[0].Notes);
        Assert.True(restored.Attachments[2].IsVehicleHardpoint);
        Assert.Equal(EngineCustomizationTrack.Load, restored.Attachments[3].EngineTrack);
        Assert.IsType<Equipment>(restored.Attachments[0].Embedded);
        Assert.Equal("Smart Armor", restored.Attachments[0].Embedded!.Name);
    }

    [Fact]
    public void RoundTrip_Cyberlimb_WithEmbeddedSmartlink_PreservesNestedCyberwareType()
    {
        var smartlink = MakeChild(capacityCost: 1m, name: "Smartlink II");
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.CyberwareCapacity,
            CapacityCost = 1m,
            Embedded = smartlink,
        });

        var json = JsonSerializer.Serialize<Equipment>(limb, JsonOptions);
        var restored = JsonSerializer.Deserialize<Equipment>(json, JsonOptions) as Cyberware;

        Assert.NotNull(restored);
        var slot = Assert.Single(restored!.Attachments);
        // Embedded should rehydrate as Cyberware via the existing $equipType discriminator.
        var embedded = Assert.IsType<Cyberware>(slot.Embedded);
        Assert.Equal("Smartlink II", embedded.Name);
    }

    [Fact]
    public void RoundTrip_Cyberdeck_WithReferencedPrograms_PreservesGearReferenceIdAndCost()
    {
        var deck = MakeDeck();
        var programId = Guid.NewGuid();
        deck.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.ProgramStorageMemory,
            GearReferenceId = programId,
            CapacityCost = 27m,
        });

        var json = JsonSerializer.Serialize<Equipment>(deck, JsonOptions);
        var restored = JsonSerializer.Deserialize<Equipment>(json, JsonOptions) as Cyberdeck;

        Assert.NotNull(restored);
        var slot = Assert.Single(restored!.Attachments);
        Assert.Equal(programId, slot.GearReferenceId);
        Assert.Equal(27m, slot.CapacityCost);
        Assert.Null(slot.Embedded);
    }

    // ---------- Legacy compatibility ----------

    [Fact]
    public void Legacy_CyberdeckJson_WithStoredProgramsField_DeserializesAttachmentsEmpty()
    {
        // A pre-attachment-system save would have shape like:
        //   { "$equipType": "cyberdeck", "Name": "...", "StoredPrograms": ["<guid>"], ... }
        // The new code drops the obsolete field; Attachments comes back empty.
        var legacyJson = """
        {
          "$equipType": "cyberdeck",
          "Name": "Old Deck",
          "Book": "Matrix",
          "Availability": { "TargetNumber": 0, "Interval": "Always" },
          "ActiveMemory": 200,
          "StorageMemory": 400,
          "StoredPrograms": ["00000000-0000-0000-0000-000000000001"],
          "ActivePrograms": ["00000000-0000-0000-0000-000000000001"]
        }
        """;
        var restored = JsonSerializer.Deserialize<Equipment>(legacyJson, JsonOptions) as Cyberdeck;

        Assert.NotNull(restored);
        Assert.Empty(restored!.Attachments);
        Assert.Equal(200, restored.ActiveMemory);
        Assert.Equal(400, restored.StorageMemory);
    }

    // ---------- ValidateAddition (preview API) ----------

    [Fact]
    public void ValidateAddition_HappyPath_ReturnsNoFailures_AndLeavesHostUnchanged()
    {
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 2m });
        var existingId = limb.Attachments[0].Id;

        var candidate = new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 3m };
        var failures = AttachmentValidator.ValidateAddition(limb, candidate);

        Assert.Empty(failures);
        // Host state unchanged: same single slot, candidate not retained.
        var slot = Assert.Single(limb.Attachments);
        Assert.Equal(existingId, slot.Id);
        Assert.DoesNotContain(candidate, limb.Attachments);
    }

    [Fact]
    public void ValidateAddition_FailurePath_ReturnsFailure_AndLeavesHostUnchanged()
    {
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 4m });

        var candidate = new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 2m }; // 4+2 > 5
        var failures = AttachmentValidator.ValidateAddition(limb, candidate);

        var f = Assert.Single(failures);
        Assert.Equal(CapacityKind.CyberwareCapacity, f.Kind);
        Assert.Equal(5m, f.Total);
        Assert.Equal(6m, f.Used);
        // Candidate rolled back; original slot remains alone.
        Assert.Single(limb.Attachments);
        Assert.DoesNotContain(candidate, limb.Attachments);
    }

    // ---------- Absent-kind semantics ----------

    [Fact]
    public void Validator_SlotWithKindNotInHostCapacityTotals_FailsWithDoesNotAllowMessage()
    {
        // Cyberware exposes only CyberwareCapacity. A VehicleLoadKg slot on it
        // should fail as "host does not allow that kind."
        var limb = MakeLimb(5m);
        limb.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleLoadKg, CapacityCost = 1m });

        var failures = AttachmentValidator.Validate(limb);
        var f = Assert.Single(failures);
        Assert.Equal(CapacityKind.VehicleLoadKg, f.Kind);
        Assert.Equal(0m, f.Total);
        Assert.Equal(1m, f.Used);
        Assert.Contains("does not allow", f.Message);
    }

    // ---------- CloneForPurchase resets Attachments ----------

    [Fact]
    public void Cyberware_CloneForPurchase_ResetsAttachments_AndIsolatesFromCatalog()
    {
        var catalog = MakeLimb(5m);
        catalog.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.CyberwareCapacity, CapacityCost = 1m });

        var clone = (Cyberware)catalog.CloneForPurchase();

        Assert.Empty(clone.Attachments);
        Assert.NotSame(catalog.Attachments, clone.Attachments);
        // Catalog itself is not mutated.
        Assert.Single(catalog.Attachments);
    }

    [Fact]
    public void Vehicle_CloneForPurchase_ResetsAttachments_AndIsolatesFromCatalog()
    {
        var catalog = MakeVehicle();
        catalog.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.VehicleCargoCF, CapacityCost = 2m });

        var clone = (Vehicle)catalog.CloneForPurchase();

        Assert.Empty(clone.Attachments);
        Assert.NotSame(catalog.Attachments, clone.Attachments);
        Assert.Single(catalog.Attachments);
    }

    [Fact]
    public void Firearm_CloneForPurchase_ResetsAttachments_ViaInheritedWeaponOverride()
    {
        var catalog = MakeFirearm();
        catalog.Attachments.Add(new AttachmentSlot { Kind = CapacityKind.FirearmMount, MountLocation = "Top", CapacityCost = 1m });

        var clone = (Firearm)catalog.CloneForPurchase();

        Assert.Empty(clone.Attachments);
        Assert.NotSame(catalog.Attachments, clone.Attachments);
        // Per-mount inventory still copies normally (it's intrinsic to the weapon).
        Assert.Equal(catalog.TopMountSlots, clone.TopMountSlots);
    }
}
