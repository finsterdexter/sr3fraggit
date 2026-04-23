using Avalonia.Controls;
using Avalonia.Interactivity;
using SR3Generator.Avalonia.ViewModels;

namespace SR3Generator.Avalonia.Views;

public partial class OptionsDialog : Window
{
    public OptionsDialog()
    {
        InitializeComponent();
    }

    public OptionsDialog(OptionsDialogViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OptionsDialogViewModel vm)
        {
            await vm.SaveAsync();
        }
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
