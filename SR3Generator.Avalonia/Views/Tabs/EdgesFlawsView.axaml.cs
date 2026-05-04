using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class EdgesFlawsView : UserControl
{
    public EdgesFlawsView()
    {
        InitializeComponent();
    }

    private void OnAvailableListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is EdgesFlawsViewModel vm &&
            vm.AddEdgeFlawCommand.CanExecute(null))
        {
            vm.AddEdgeFlawCommand.Execute(null);
        }
    }

    private void OnSelectedListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is EdgesFlawsViewModel vm &&
            vm.RemoveEdgeFlawCommand.CanExecute(null))
        {
            vm.RemoveEdgeFlawCommand.Execute(null);
        }
    }
}
