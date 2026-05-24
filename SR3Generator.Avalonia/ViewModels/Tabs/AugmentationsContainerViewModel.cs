using CommunityToolkit.Mvvm.ComponentModel;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Container for the Augmentations top-level tab. Catalog buys/sells
/// cyberware + bioware; Mods installs enhancements on capacity-bearing hosts
/// (cyberlimbs ECU, cybereyes/cyberears Essence pool).</summary>
public partial class AugmentationsContainerViewModel : ViewModelBase
{
    public AugmentationsViewModel CatalogVM { get; }
    public AugmentModsViewModel ModsVM { get; }

    [ObservableProperty]
    private int _selectedSubtabIndex;

    public AugmentationsContainerViewModel(AugmentationsViewModel catalogVM, AugmentModsViewModel modsVM)
    {
        CatalogVM = catalogVM;
        ModsVM = modsVM;
    }
}
