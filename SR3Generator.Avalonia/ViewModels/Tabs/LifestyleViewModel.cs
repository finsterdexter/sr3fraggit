using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class LifestyleViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    public ObservableCollection<LifestyleTier> Tiers { get; } = new(LifestyleDatabase.Tiers);

    [ObservableProperty]
    private LifestyleTier _selectedTier = LifestyleTier.Middle;

    [ObservableProperty]
    private int _months = 1;

    [ObservableProperty]
    private bool _isPermanent;

    [ObservableProperty]
    private ObservableCollection<LifestyleItem> _owned = new();

    [ObservableProperty]
    private LifestyleItem? _selectedLifestyle;

    [ObservableProperty]
    private long _nuyenSpentOnLifestyle;

    public LifestyleViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();

    // Number of months charged for the current selection (permanent = 100).
    public int EffectiveMonths => IsPermanent ? LifestyleDatabase.PermanentMonths : Math.Max(1, Months);
    public int MonthlyCost => SelectedTier.GetMonthlyCost();
    public long TotalCost => (long)MonthlyCost * EffectiveMonths;

    partial void OnSelectedTierChanged(LifestyleTier value) => NotifyCost();
    partial void OnMonthsChanged(int value) => NotifyCost();
    partial void OnIsPermanentChanged(bool value) => NotifyCost();

    private void NotifyCost()
    {
        OnPropertyChanged(nameof(EffectiveMonths));
        OnPropertyChanged(nameof(MonthlyCost));
        OnPropertyChanged(nameof(TotalCost));
    }

    private void RefreshFromBuilder()
    {
        var character = _characterService.Builder.Character;
        Owned.Clear();
        NuyenSpentOnLifestyle = 0;
        foreach (var lifestyle in character.Lifestyles)
        {
            Owned.Add(new LifestyleItem(lifestyle));
            NuyenSpentOnLifestyle += (long)lifestyle.MonthlyCost * lifestyle.MonthsPaid;
        }
        NotifyCost();
    }

    [RelayCommand]
    private void Buy()
    {
        _characterService.BuyLifestyle(SelectedTier, EffectiveMonths);
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedLifestyle is null) return;
        _characterService.RemoveLifestyle(SelectedLifestyle.Source);
    }
}

public class LifestyleItem
{
    public Lifestyle Source { get; }
    public string TierDisplay => Source.Tier.ToString();
    public bool IsPermanent => Source.MonthsPaid >= LifestyleDatabase.PermanentMonths;
    public string DurationDisplay => IsPermanent ? "Permanent" : $"{Source.MonthsPaid} mo";
    public string CostDisplay => $"{(long)Source.MonthlyCost * Source.MonthsPaid:N0}¥";

    public LifestyleItem(Lifestyle source)
    {
        Source = source;
    }
}
