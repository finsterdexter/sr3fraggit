using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class SpiritsView : UserControl
{
    public SpiritsView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SpiritsViewModel vm &&
            !string.IsNullOrEmpty(vm.SelectedSpiritType) &&
            vm.AddSpiritCommand.CanExecute(null))
        {
            vm.AddSpiritCommand.Execute(null);
        }
    }

    private void OnBoundListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SpiritsViewModel vm &&
            vm.SelectedBoundSpirit is not null &&
            vm.RemoveSpiritCommand.CanExecute(null))
        {
            vm.RemoveSpiritCommand.Execute(null);
        }
    }
}
