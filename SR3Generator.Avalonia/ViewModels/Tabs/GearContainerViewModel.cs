using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Container for the Gear top-level tab. Inner sub-tabs split the
/// catalog (buy/sell firearms and general gear) from the mods experience
/// (attach accessories / modifications to owned firearms).</summary>
public partial class GearContainerViewModel : ViewModelBase
{
    public GearViewModel CatalogVM { get; }
    public GearModsViewModel ModsVM { get; }

    [ObservableProperty]
    private int _selectedSubtabIndex;

    public GearContainerViewModel(GearViewModel catalogVM, GearModsViewModel modsVM)
    {
        CatalogVM = catalogVM;
        ModsVM = modsVM;
        CatalogVM.OpenModsRequested += OnOpenModsRequested;
    }

    // Catalog "Mods" button → flip to the Mods sub-tab (index 1) and select the firearm.
    private void OnOpenModsRequested(Guid firearmId)
    {
        SelectedSubtabIndex = 1;
        ModsVM.SelectFirearm(firearmId);
    }
}
