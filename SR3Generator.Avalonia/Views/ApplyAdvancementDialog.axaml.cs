using Avalonia.Controls;
using Avalonia.Interactivity;
using SR3Generator.Avalonia.ViewModels;

namespace SR3Generator.Avalonia.Views;

public partial class ApplyAdvancementDialog : Window
{
    public ApplyAdvancementDialog()
    {
        InitializeComponent();
    }

    public ApplyAdvancementDialog(ApplyAdvancementDialogViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
