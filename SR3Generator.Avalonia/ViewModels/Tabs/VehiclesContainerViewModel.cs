using CommunityToolkit.Mvvm.ComponentModel;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Container for the Vehicles top-level tab. Catalog handles buy/sell
/// of the vehicle itself; Mods handles installed modifications and weapon
/// mounts.</summary>
public partial class VehiclesContainerViewModel : ViewModelBase
{
    public VehiclesViewModel CatalogVM { get; }
    public VehicleModsViewModel ModsVM { get; }

    [ObservableProperty]
    private int _selectedSubtabIndex;

    public VehiclesContainerViewModel(VehiclesViewModel catalogVM, VehicleModsViewModel modsVM)
    {
        CatalogVM = catalogVM;
        ModsVM = modsVM;
    }
}
