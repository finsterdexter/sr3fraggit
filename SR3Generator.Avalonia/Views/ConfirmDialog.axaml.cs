using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SR3Generator.Avalonia.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message, bool isError) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        if (isError)
        {
            CancelButton.IsVisible = false;
            OkButton.Content = "OK";
        }
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
