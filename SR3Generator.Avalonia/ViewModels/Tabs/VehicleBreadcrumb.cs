using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Breadcrumb-filter helpers shared by the Vehicles catalog tab and
/// the Vehicle Mods tab. Renders a chain of dropdowns from the catalog's
/// category-tree paths, skipping any depth where the user has only one option
/// (so locked-in roots like "Vehicles" or "Vehicle Gear" don't waste a
/// dropdown).</summary>
internal static class VehicleBreadcrumb
{
    public static List<string> OptionsAtDepth(
        IReadOnlyList<string[]> allPaths,
        IReadOnlyList<string> selectedPath,
        int depth)
    {
        return allPaths
            .Where(p => p.Length > depth)
            .Where(p =>
            {
                for (int i = 0; i < depth && i < selectedPath.Count; i++)
                    if (!p[i].Equals(selectedPath[i], StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            })
            .Select(p => p[depth])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    public static bool MatchesPath(IReadOnlyList<string> tree, IReadOnlyList<string> selected)
    {
        if (selected.Count == 0) return true;
        if (tree.Count < selected.Count) return false;
        for (int i = 0; i < selected.Count; i++)
            if (!tree[i].Equals(selected[i], StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public static void Rebuild(
        ObservableCollection<BreadcrumbStep> steps,
        IReadOnlyList<string[]> allPaths,
        IReadOnlyList<string> selectedPath,
        Action<int, string?> onChanged)
    {
        steps.Clear();
        for (int depth = 0; depth < selectedPath.Count; depth++)
        {
            var options = OptionsAtDepth(allPaths, selectedPath, depth);
            if (options.Count <= 1) continue;
            var step = new BreadcrumbStep(depth, options, onChanged);
            step.SetSilently(selectedPath[depth]);
            steps.Add(step);
        }
        var nextDepth = selectedPath.Count;
        var nextOptions = OptionsAtDepth(allPaths, selectedPath, nextDepth);
        if (nextOptions.Count > 1)
            steps.Add(new BreadcrumbStep(nextDepth, nextOptions, onChanged));
        if (steps.Count > 0)
            steps[0].IsFirstVisible = true;
    }
}
