using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class BiowareView : UserControl
{
    public BiowareView()
    {
        InitializeComponent();
    }

    private void OnBiowareListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AugmentationsViewModel vm &&
            vm.SelectedBiowareItem is not null &&
            vm.InstallBiowareCommand.CanExecute(null))
        {
            vm.InstallBiowareCommand.Execute(null);
        }
    }

    private void OnInstalledListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AugmentationsViewModel vm &&
            vm.SelectedInstalledAug is not null &&
            vm.RemoveAugmentationCommand.CanExecute(null))
        {
            vm.RemoveAugmentationCommand.Execute(null);
        }
    }
}
