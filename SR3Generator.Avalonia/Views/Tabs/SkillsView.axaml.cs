using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class SkillsView : UserControl
{
    public SkillsView()
    {
        InitializeComponent();
    }

    // Double-tap in either list adds or removes one skill level. Available = +1 (adds the skill
    // at rating 1 if not yet purchased); Purchased = -1 (removes the skill when going below 1).
    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SkillsViewModel vm &&
            vm.IncrementDetailRatingCommand.CanExecute(null))
        {
            vm.IncrementDetailRatingCommand.Execute(null);
        }
    }

    private void OnPurchasedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SkillsViewModel vm &&
            vm.DecrementDetailRatingCommand.CanExecute(null))
        {
            vm.DecrementDetailRatingCommand.Execute(null);
        }
    }
}
