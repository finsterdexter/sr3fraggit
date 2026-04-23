using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Database;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class PrioritiesViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    public ObservableCollection<PriorityRow> OrderedPriorities { get; } = new();

    public PrioritiesViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;

        // Order the rows by the builder's current ranks so loaded characters show their
        // actual priority layout rather than being clobbered by a hardcoded default.
        // Rank A (enum value 4) goes on top, rank E (value 0) on the bottom.
        var current = _characterService.Builder.Priorities;
        foreach (var p in current.OrderByDescending(p => (int)p.Rank))
        {
            OrderedPriorities.Add(new PriorityRow(p.Type));
        }

        RefreshRanks();
        // Intentionally no ApplyPriorities() here — we're reflecting existing builder state,
        // not mutating it. Only user-driven reorders should push back through the service.
    }

    public void MovePriority(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= OrderedPriorities.Count) return;
        if (toIndex < 0 || toIndex >= OrderedPriorities.Count) return;

        OrderedPriorities.Move(fromIndex, toIndex);
        RefreshRanks();
        ApplyPriorities();
    }

    [RelayCommand]
    private void MoveUp(PriorityRow? row)
    {
        if (row is null) return;
        var idx = OrderedPriorities.IndexOf(row);
        if (idx > 0) MovePriority(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown(PriorityRow? row)
    {
        if (row is null) return;
        var idx = OrderedPriorities.IndexOf(row);
        if (idx >= 0 && idx < OrderedPriorities.Count - 1) MovePriority(idx, idx + 1);
    }

    private void RefreshRanks()
    {
        // Top of list (index 0) = rank A (enum value 4); bottom (index 4) = rank E (enum value 0)
        for (int i = 0; i < OrderedPriorities.Count; i++)
        {
            OrderedPriorities[i].SetRank((PriorityRank)(4 - i));
        }
    }

    private void ApplyPriorities()
    {
        var priorities = OrderedPriorities
            .Select(r => new Priority(r.Type, r.Rank))
            .ToList();
        _characterService.SetPriorities(priorities);
    }
}

public partial class PriorityRow : ObservableObject
{
    [ObservableProperty] private PriorityRank _rank;
    [ObservableProperty] private string _benefits = string.Empty;
    [ObservableProperty] private string _rankDisplay = "A";
    [ObservableProperty] private bool _isDragging;

    public PriorityType Type { get; }
    public string DisplayName { get; }
    public string AccentResource { get; }

    public PriorityRow(PriorityType type)
    {
        Type = type;
        DisplayName = type.ToString();
        AccentResource = type switch
        {
            PriorityType.Magic => "AccentManaBrush",
            PriorityType.Resources => "AccentNuyenBrush",
            _ => "InkPrimaryBrush"
        };
    }

    public void SetRank(PriorityRank rank)
    {
        Rank = rank;
        RankDisplay = rank.ToString();
        Benefits = ComputeBenefits(Type, rank);
    }

    private static string ComputeBenefits(PriorityType type, PriorityRank rank)
    {
        var priority = new Priority(type, rank);
        return type switch
        {
            PriorityType.Race => GetRaceBenefits(rank),
            PriorityType.Magic => GetMagicBenefits(rank),
            PriorityType.Attributes => $"{priority.GetAttributePoints()} attribute points",
            PriorityType.Skills => $"{priority.GetSkillPoints()} skill points",
            PriorityType.Resources => $"{priority.GetNuyen():N0}¥",
            _ => string.Empty
        };
    }

    private static string GetRaceBenefits(PriorityRank rank)
    {
        var races = new Priority(PriorityType.Race, rank).GetAllowedRaces();
        return string.Join(", ", races.Select(r => r.Name.ToString()));
    }

    private static string GetMagicBenefits(PriorityRank rank)
    {
        var aspects = new Priority(PriorityType.Magic, rank).GetAllowedMagicAspects();
        if (aspects.All(a => a.Name == AspectName.Mundane))
            return "Mundane (No Magic)";
        return string.Join(", ", aspects.Where(a => a.Name != AspectName.Mundane).Select(a => a.Name.ToString()));
    }
}
