using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class FociView : UserControl
{
    public FociView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is FociViewModel vm &&
            vm.SelectedAvailableFocus is not null &&
            vm.BuyFocusCommand.CanExecute(null))
        {
            vm.BuyFocusCommand.Execute(null);
        }
    }

    private void OnOwnedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is FociViewModel vm &&
            vm.SelectedOwnedFocus is not null &&
            vm.SellFocusCommand.CanExecute(null))
        {
            vm.SellFocusCommand.Execute(null);
        }
    }
}
