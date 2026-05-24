using CommunityToolkit.Mvvm.ComponentModel;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Container for the Matrix top-level tab. Catalog buys/sells
/// cyberdecks and programs; Mods manages deck loadouts (Storage/Active
/// memory) and persona BEMS tuning.</summary>
public partial class MatrixContainerViewModel : ViewModelBase
{
    public MatrixViewModel CatalogVM { get; }
    public MatrixModsViewModel ModsVM { get; }

    [ObservableProperty]
    private int _selectedSubtabIndex;

    public MatrixContainerViewModel(MatrixViewModel catalogVM, MatrixModsViewModel modsVM)
    {
        CatalogVM = catalogVM;
        ModsVM = modsVM;
    }
}
