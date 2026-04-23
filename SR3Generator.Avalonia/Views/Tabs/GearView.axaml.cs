using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class GearView : UserControl
{
    public GearView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is GearViewModel vm &&
            vm.SelectedGearItem is not null &&
            vm.BuyGearCommand.CanExecute(null))
        {
            vm.BuyGearCommand.Execute(null);
        }
    }

    private void OnOwnedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is GearViewModel vm &&
            vm.SelectedOwnedGear is not null &&
            vm.SellGearCommand.CanExecute(null))
        {
            vm.SellGearCommand.Execute(null);
        }
    }
}
