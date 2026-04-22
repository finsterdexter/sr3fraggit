using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using SR3Generator.Avalonia.ViewModels.Tabs;
using System;
using System.Collections.Generic;

namespace SR3Generator.Avalonia.Views.Tabs;

public partial class PrioritiesView : UserControl
{
    private const double DragThreshold = 4.0;

    private PriorityRow? _pressedRow;
    private Point _pressPointInHost;
    private Point _pressOffsetInRow;
    private bool _isDragging;

    public PrioritiesView()
    {
        InitializeComponent();
    }

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not PriorityRow row) return;
        if (!e.GetCurrentPoint(border).Properties.IsLeftButtonPressed) return;

        // Let nudge buttons do their own thing.
        if (e.Source is Visual src &&
            (src is Button || src.FindAncestorOfType<Button>() != null))
        {
            return;
        }

        _pressedRow = row;
        _pressPointInHost = e.GetPosition(DragHost);
        _pressOffsetInRow = e.GetPosition(border);
        _isDragging = false;

        // Capture to the host so move/release still fires even as rows are reordered underneath.
        e.Pointer.Capture(DragHost);
        e.Handled = true;
    }

    private void OnHostPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedRow is null) return;

        var posInHost = e.GetPosition(DragHost);

        if (!_isDragging)
        {
            var dx = posInHost.X - _pressPointInHost.X;
            var dy = posInHost.Y - _pressPointInHost.Y;
            if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold) return;

            StartDrag();
        }

        if (!_isDragging) return;

        PositionGhost(posInHost);
        MaybeReorder(posInHost);
    }

    private void OnHostPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndDrag(e.Pointer);
    }

    private void OnHostPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        EndDrag(e.Pointer);
    }

    private void StartDrag()
    {
        if (_pressedRow is null) return;
        var sourceBorder = FindBorderFor(_pressedRow);
        if (sourceBorder is null) return;

        _isDragging = true;
        _pressedRow.IsDragging = true;

        // Snapshot the source row as a bitmap so the ghost looks identical.
        var size = sourceBorder.Bounds.Size;
        var w = Math.Max(1, (int)Math.Ceiling(size.Width));
        var h = Math.Max(1, (int)Math.Ceiling(size.Height));
        var bitmap = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
        bitmap.Render(sourceBorder);

        DragGhost.Source = bitmap;
        DragGhost.Width = size.Width;
        DragGhost.Height = size.Height;
        DragGhost.IsVisible = true;
    }

    private void PositionGhost(Point posInHost)
    {
        var x = posInHost.X - _pressOffsetInRow.X;
        var y = posInHost.Y - _pressOffsetInRow.Y;
        Canvas.SetLeft(DragGhost, x);
        Canvas.SetTop(DragGhost, y);
    }

    private void MaybeReorder(Point posInHost)
    {
        if (_pressedRow is null || DataContext is not PrioritiesViewModel vm) return;

        // Collect the current row borders in list order.
        var rows = GetRowBorders();
        if (rows.Count == 0) return;

        var currentIndex = vm.OrderedPriorities.IndexOf(_pressedRow);
        if (currentIndex < 0) return;

        // Cursor Y in host coords — find which slot it belongs in.
        var cursorY = posInHost.Y;

        // Compute the midpoint Y of each row in host coords.
        int targetIndex = rows.Count - 1;
        for (int i = 0; i < rows.Count; i++)
        {
            var border = rows[i];
            var topLeft = border.TranslatePoint(new Point(0, 0), DragHost);
            if (topLeft is null) continue;
            var midY = topLeft.Value.Y + border.Bounds.Height / 2.0;
            if (cursorY < midY)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == currentIndex) return;
        vm.MovePriority(currentIndex, targetIndex);
    }

    private void EndDrag(IPointer pointer)
    {
        if (_pressedRow is not null)
        {
            _pressedRow.IsDragging = false;
        }
        DragGhost.IsVisible = false;
        DragGhost.Source = null;
        pointer.Capture(null);
        _pressedRow = null;
        _isDragging = false;
    }

    private Border? FindBorderFor(PriorityRow row)
    {
        foreach (var b in GetRowBorders())
        {
            if (b.DataContext == row) return b;
        }
        return null;
    }

    private List<Border> GetRowBorders()
    {
        var result = new List<Border>();
        foreach (var visual in PriorityList.GetVisualDescendants())
        {
            if (visual is Border b && b.Classes.Contains("priority-row"))
            {
                result.Add(b);
            }
        }
        return result;
    }
}
