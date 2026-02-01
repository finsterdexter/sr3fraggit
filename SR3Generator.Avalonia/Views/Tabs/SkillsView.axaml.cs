using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SR3Generator.Avalonia.ViewModels.Tabs;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class SkillsView : UserControl
{
    public SkillsView()
    {
        InitializeComponent();
    }

    private void OnAvailableSkillPressed(object? sender, PointerPressedEventArgs e)
    {
        // Don't trigger if clicking on the expand button
        if (e.Source is Button) return;

        if (sender is Border border && border.DataContext is AvailableSkillItem skillItem)
        {
            skillItem.AddCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnPurchasedSpecPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is AvailableSpecItem specItem)
        {
            // Only handle fixed specs (not user-entry)
            if (!specItem.RequiresUserInput)
            {
                // Find the parent PurchasedSkillItem
                var border = grid.FindAncestorOfType<Border>();
                var itemsControl = border?.FindAncestorOfType<ItemsControl>();
                var parent = itemsControl?.FindAncestorOfType<StackPanel>()?.DataContext;
                if (parent is PurchasedSkillItem purchasedSkill)
                {
                    purchasedSkill.SelectSpecialization(specItem);
                    e.Handled = true;
                }
            }
        }
    }

    private void OnSpecNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            AddCustomSpecFromTextBox(textBox);
            e.Handled = true;
        }
    }

    private void OnAddCustomSpecClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Find the sibling TextBox
            var grid = button.FindAncestorOfType<Grid>();
            var textBox = grid?.FindDescendantOfType<TextBox>();
            if (textBox != null)
            {
                AddCustomSpecFromTextBox(textBox);
            }
        }
    }

    private void AddCustomSpecFromTextBox(TextBox textBox)
    {
        var customName = textBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(customName)) return;

        if (textBox.DataContext is AvailableSpecItem specItem)
        {
            // Find the parent PurchasedSkillItem
            var border = textBox.FindAncestorOfType<Border>();
            var itemsControl = border?.FindAncestorOfType<ItemsControl>();
            var parent = itemsControl?.FindAncestorOfType<StackPanel>()?.DataContext;
            if (parent is PurchasedSkillItem purchasedSkill)
            {
                purchasedSkill.SelectSpecialization(specItem, customName);
                textBox.Text = string.Empty; // Clear the input
            }
        }
    }
}
