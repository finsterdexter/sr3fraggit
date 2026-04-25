using Avalonia.Controls;
using Avalonia.Input;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class MatrixView : UserControl
{
    public MatrixView()
    {
        InitializeComponent();
    }

    private void OnAvailableDecksDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MatrixViewModel vm &&
            vm.SelectedDeckCatalogItem is not null &&
            vm.BuyDeckCommand.CanExecute(null))
        {
            vm.BuyDeckCommand.Execute(null);
        }
    }
}
