using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class AdeptPowersView : UserControl
{
    public AdeptPowersView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AdeptPowersViewModel vm &&
            vm.SelectedAvailablePower is not null &&
            vm.AddPowerCommand.CanExecute(null))
        {
            vm.AddPowerCommand.Execute(null);
        }
    }

    private void OnPurchasedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AdeptPowersViewModel vm &&
            vm.SelectedPurchasedPower is not null &&
            vm.RemovePowerCommand.CanExecute(null))
        {
            vm.RemovePowerCommand.Execute(null);
        }
    }
}
