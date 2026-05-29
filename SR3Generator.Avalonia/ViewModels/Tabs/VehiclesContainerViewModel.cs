using System;
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
        CatalogVM.OpenModsRequested += OnOpenModsRequested;
    }

    // Catalog "Mods" button → flip to the Mods sub-tab (index 1) and select the vehicle.
    private void OnOpenModsRequested(Guid vehicleId)
    {
        SelectedSubtabIndex = 1;
        ModsVM.SelectVehicle(vehicleId);
    }
}
