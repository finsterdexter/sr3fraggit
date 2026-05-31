using Avalonia.Controls;
using Avalonia.Interactivity;
using SR3Generator.Avalonia.ViewModels;

namespace SR3Generator.Avalonia.Views;

public partial class KarmaConversionDialog : Window
{
    public KarmaConversionDialog()
    {
        InitializeComponent();
    }

    public KarmaConversionDialog(KarmaConversionDialogViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        if (DataContext is KarmaConversionDialogViewModel vm)
        {
            await vm.SaveAsync();
        }
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
