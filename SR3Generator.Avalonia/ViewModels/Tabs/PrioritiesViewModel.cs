using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class PrioritiesViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private bool _isUpdating;

    [ObservableProperty]
    private PriorityRank _racePriority = PriorityRank.A;

    [ObservableProperty]
    private PriorityRank _magicPriority = PriorityRank.B;

    [ObservableProperty]
    private PriorityRank _attributesPriority = PriorityRank.C;

    [ObservableProperty]
    private PriorityRank _skillsPriority = PriorityRank.D;

    [ObservableProperty]
    private PriorityRank _resourcesPriority = PriorityRank.E;

    // Benefits descriptions
    [ObservableProperty]
    private string _raceBenefits = string.Empty;

    [ObservableProperty]
    private string _magicBenefits = string.Empty;

    [ObservableProperty]
    private string _attributesBenefits = string.Empty;

    [ObservableProperty]
    private string _skillsBenefits = string.Empty;

    [ObservableProperty]
    private string _resourcesBenefits = string.Empty;

    public ObservableCollection<PriorityRank> AvailableRanks { get; } = new(Enum.GetValues<PriorityRank>());

    public PrioritiesViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        RefreshBenefits();
        ApplyPriorities();
    }

    partial void OnRacePriorityChanged(PriorityRank value)
    {
        if (!_isUpdating)
            HandlePriorityChange(PriorityType.Race, value);
    }

    partial void OnMagicPriorityChanged(PriorityRank value)
    {
        if (!_isUpdating)
            HandlePriorityChange(PriorityType.Magic, value);
    }

    partial void OnAttributesPriorityChanged(PriorityRank value)
    {
        if (!_isUpdating)
            HandlePriorityChange(PriorityType.Attributes, value);
    }

    partial void OnSkillsPriorityChanged(PriorityRank value)
    {
        if (!_isUpdating)
            HandlePriorityChange(PriorityType.Skills, value);
    }

    partial void OnResourcesPriorityChanged(PriorityRank value)
    {
        if (!_isUpdating)
            HandlePriorityChange(PriorityType.Resources, value);
    }

    private void HandlePriorityChange(PriorityType changedType, PriorityRank newRank)
    {
        // Find which other priority has the rank we want and swap
        var currentPriorities = GetCurrentPriorities();
        var conflicting = currentPriorities.FirstOrDefault(p => p.Type != changedType && p.Rank == newRank);

        if (conflicting != null)
        {
            // Get the old rank of the changed type
            var oldRank = currentPriorities.First(p => p.Type == changedType).Rank;

            _isUpdating = true;
            try
            {
                // Swap the conflicting priority to our old rank
                SetPriorityRank(conflicting.Type, oldRank);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        RefreshBenefits();
        ApplyPriorities();
    }

    private void SetPriorityRank(PriorityType type, PriorityRank rank)
    {
        switch (type)
        {
            case PriorityType.Race:
                RacePriority = rank;
                break;
            case PriorityType.Magic:
                MagicPriority = rank;
                break;
            case PriorityType.Attributes:
                AttributesPriority = rank;
                break;
            case PriorityType.Skills:
                SkillsPriority = rank;
                break;
            case PriorityType.Resources:
                ResourcesPriority = rank;
                break;
        }
    }

    private List<Priority> GetCurrentPriorities()
    {
        return new List<Priority>
        {
            new Priority(PriorityType.Race, RacePriority),
            new Priority(PriorityType.Magic, MagicPriority),
            new Priority(PriorityType.Attributes, AttributesPriority),
            new Priority(PriorityType.Skills, SkillsPriority),
            new Priority(PriorityType.Resources, ResourcesPriority)
        };
    }

    private void RefreshBenefits()
    {
        RaceBenefits = GetRaceBenefits(RacePriority);
        MagicBenefits = GetMagicBenefits(MagicPriority);
        AttributesBenefits = $"{new Priority(PriorityType.Attributes, AttributesPriority).GetAttributePoints()} points";
        SkillsBenefits = $"{new Priority(PriorityType.Skills, SkillsPriority).GetSkillPoints()} points";
        ResourcesBenefits = $"{new Priority(PriorityType.Resources, ResourcesPriority).GetNuyen():N0}¥";
    }

    private string GetRaceBenefits(PriorityRank rank)
    {
        var races = new Priority(PriorityType.Race, rank).GetAllowedRaces();
        return string.Join(", ", races.Select(r => r.Name.ToString()));
    }

    private string GetMagicBenefits(PriorityRank rank)
    {
        var aspects = new Priority(PriorityType.Magic, rank).GetAllowedMagicAspects();
        if (aspects.All(a => a.Name == AspectName.Mundane))
            return "Mundane (No Magic)";
        return string.Join(", ", aspects.Where(a => a.Name != AspectName.Mundane).Select(a => a.Name.ToString()));
    }

    private void ApplyPriorities()
    {
        var priorities = GetCurrentPriorities();
        _characterService.SetPriorities(priorities);
    }
}
