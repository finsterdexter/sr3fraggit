using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class SpellsView : UserControl
{
    public SpellsView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SpellsViewModel vm &&
            vm.SelectedAvailableSpell is not null &&
            vm.AddSpellCommand.CanExecute(null))
        {
            vm.AddSpellCommand.Execute(null);
        }
    }

    private void OnPurchasedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SpellsViewModel vm &&
            vm.SelectedPurchasedSpell is not null &&
            vm.RemoveSpellCommand.CanExecute(null))
        {
            vm.RemoveSpellCommand.Execute(null);
        }
    }
}
